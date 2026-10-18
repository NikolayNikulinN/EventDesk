using EventDesk.Application.Reminders;
using EventDesk.Constants;
using EventDesk.Domain.Events;
using EventDesk.Domain.Reminders;
using Microsoft.EntityFrameworkCore;

namespace EventDesk.Infrastructure.Persistence;

public sealed class ReminderStore(EventDeskDbContext dbContext) : IReminderStore
{
    public async Task PrepareAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        List<CalendarEvent> events = await dbContext.CalendarEvents.AsNoTracking()
            .ToListAsync(cancellationToken);
        List<ReminderDeliveryRecord> deliveries = await dbContext.ReminderDeliveries
            .ToListAsync(cancellationToken);
        Dictionary<(Guid EventId, ReminderChannel Channel, long DueTicks), ReminderDeliveryRecord> known =
            deliveries.ToDictionary(item => (item.EventId, item.Channel, item.DueAtUtc.UtcTicks));
        Dictionary<Guid, CalendarEvent> eventsById = events.ToDictionary(item => item.Id);

        foreach (ReminderDeliveryRecord delivery in deliveries.Where(item =>
                     item.Status is ReminderDeliveryStatus.Pending or ReminderDeliveryStatus.Delivering))
        {
            if (!eventsById.TryGetValue(delivery.EventId, out CalendarEvent? calendarEvent) ||
                !IsScheduled(calendarEvent, delivery))
            {
                delivery.Status = ReminderDeliveryStatus.Canceled;
                delivery.LeaseToken = null;
                delivery.LeaseUntilUtc = null;
                ReminderAttemptRecord? started = await dbContext.ReminderAttempts
                    .SingleOrDefaultAsync(item => item.DeliveryId == delivery.Id &&
                        item.Number == delivery.AttemptCount &&
                        item.Status == ReminderAttemptStatus.Started, cancellationToken);
                if (started is not null)
                {
                    started.Status = ReminderAttemptStatus.Interrupted;
                    started.FinishedAtUtc = now;
                    started.ErrorCode = ReminderProcessingConstants.Interrupted;
                }
            }
            else
            {
                delivery.EventTitle = calendarEvent.Title;
            }
        }

        ReminderScheduleCursor? cursor = await dbContext.ReminderScheduleCursors
            .SingleOrDefaultAsync(cancellationToken);
        DateTimeOffset from = cursor?.ThroughUtc ?? now.AddDays(-1);
        DateTimeOffset through = from.AddDays(31) < now.AddDays(1)
            ? from.AddDays(31) : now.AddDays(1);

        foreach (CalendarEvent calendarEvent in events.Where(item => item.IsEnabled && !item.IsDeleted))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (calendarEvent.RecurrenceKind == EventRecurrenceKind.None)
            {
                foreach (ReminderOccurrence occurrence in calendarEvent.GetReminderSchedule())
                {
                    Upsert(calendarEvent, occurrence.Channel, occurrence.TriggerAtUtc, known);
                }
            }
            else if (through > from)
            {
                TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(calendarEvent.TimeZoneId);
                DateOnly first = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(from, zone).Date);
                DateOnly last = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(through, zone).Date);
                for (DateOnly date = first; date <= last; date = date.AddDays(1))
                {
                    foreach (DateTimeOffset due in calendarEvent.GetRecurringNotificationTimes(date))
                    {
                        if (due >= from && due <= through)
                            Upsert(calendarEvent, ReminderChannel.Desktop, due, known);
                    }
                }
            }
        }

        if (through > from)
        {
            if (cursor is null)
                dbContext.ReminderScheduleCursors.Add(new ReminderScheduleCursor { ThroughUtc = through });
            else
                cursor.ThroughUtc = through;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReminderClaim?> ClaimAsync(DateTimeOffset now, TimeSpan lease,
        int maxAttempts, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        while (true)
        {
            ReminderDeliveryRecord? delivery = await dbContext.ReminderDeliveries
                .Where(item => item.DueAtUtc <= now &&
                    ((item.Status == ReminderDeliveryStatus.Pending && item.NextAttemptAtUtc <= now) ||
                     (item.Status == ReminderDeliveryStatus.Delivering && item.LeaseUntilUtc <= now)))
                .OrderBy(item => item.DueAtUtc).ThenBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (delivery is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            if (delivery.Status == ReminderDeliveryStatus.Delivering)
            {
                ReminderAttemptRecord? interrupted = await dbContext.ReminderAttempts
                    .SingleOrDefaultAsync(item => item.DeliveryId == delivery.Id &&
                        item.Number == delivery.AttemptCount, cancellationToken);
                if (interrupted is not null && interrupted.Status == ReminderAttemptStatus.Started)
                {
                    interrupted.Status = ReminderAttemptStatus.Interrupted;
                    interrupted.FinishedAtUtc = now;
                    interrupted.ErrorCode = ReminderProcessingConstants.Interrupted;
                }
            }

            if (delivery.AttemptCount >= maxAttempts)
            {
                delivery.Status = ReminderDeliveryStatus.Failed;
                delivery.LeaseToken = null;
                delivery.LeaseUntilUtc = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            delivery.AttemptCount++;
            delivery.Status = ReminderDeliveryStatus.Delivering;
            delivery.LeaseToken = Guid.NewGuid();
            delivery.LeaseUntilUtc = now + lease;
            dbContext.ReminderAttempts.Add(new ReminderAttemptRecord
            {
                Id = Guid.NewGuid(), DeliveryId = delivery.Id,
                Number = delivery.AttemptCount, StartedAtUtc = now,
                Status = ReminderAttemptStatus.Started
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            string? description = await dbContext.CalendarEvents.AsNoTracking()
                .Where(item => item.Id == delivery.EventId)
                .Select(item => item.Description)
                .SingleOrDefaultAsync(cancellationToken);
            return new ReminderClaim(delivery.Id, delivery.LeaseToken.Value,
                delivery.EventId, delivery.EventTitle, description, delivery.Channel,
                delivery.DueAtUtc, delivery.AttemptCount);
        }
    }

    public async Task<bool> CompleteAsync(ReminderClaim claim, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await FinishAsync(claim, now, null, false, null, cancellationToken);

    public async Task<bool> FailAsync(ReminderClaim claim, DateTimeOffset now,
        DateTimeOffset nextAttemptAt, bool permanent, string errorCode,
        CancellationToken cancellationToken) =>
        await FinishAsync(claim, now, nextAttemptAt, permanent, errorCode, cancellationToken);

    public async Task<IReadOnlyList<ReminderHistoryItem>> GetHistoryAsync(int limit,
        CancellationToken cancellationToken) =>
        await dbContext.ReminderDeliveries.AsNoTracking()
            .OrderByDescending(item => item.DueAtUtc).ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 500))
            .Select(item => new ReminderHistoryItem(item.Id, item.EventTitle,
                item.Channel, item.DueAtUtc, item.Status, item.AttemptCount,
                item.DeliveredAtUtc, item.LastErrorCode))
            .ToListAsync(cancellationToken);

    private async Task<bool> FinishAsync(ReminderClaim claim, DateTimeOffset now,
        DateTimeOffset? nextAttemptAt, bool permanent, string? errorCode,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        ReminderDeliveryRecord? delivery = await dbContext.ReminderDeliveries
            .SingleOrDefaultAsync(item => item.Id == claim.DeliveryId, cancellationToken);
        if (delivery is null || delivery.Status != ReminderDeliveryStatus.Delivering ||
            delivery.LeaseToken != claim.LeaseToken)
            return false;

        ReminderAttemptRecord attempt = await dbContext.ReminderAttempts
            .SingleAsync(item => item.DeliveryId == claim.DeliveryId &&
                item.Number == claim.AttemptNumber, cancellationToken);
        attempt.FinishedAtUtc = now;
        attempt.Status = errorCode is null ? ReminderAttemptStatus.Succeeded : ReminderAttemptStatus.Failed;
        attempt.ErrorCode = errorCode;
        delivery.Status = errorCode is null ? ReminderDeliveryStatus.Delivered :
            permanent ? ReminderDeliveryStatus.Failed : ReminderDeliveryStatus.Pending;
        delivery.DeliveredAtUtc = errorCode is null ? now : null;
        delivery.NextAttemptAtUtc = nextAttemptAt ?? delivery.NextAttemptAtUtc;
        delivery.LastErrorCode = errorCode;
        delivery.LeaseToken = null;
        delivery.LeaseUntilUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void Upsert(CalendarEvent calendarEvent, ReminderChannel channel,
        DateTimeOffset due, Dictionary<(Guid, ReminderChannel, long), ReminderDeliveryRecord> known)
    {
        var key = (calendarEvent.Id, channel, due.UtcTicks);
        if (known.TryGetValue(key, out ReminderDeliveryRecord? existing))
        {
            if (existing.Status == ReminderDeliveryStatus.Canceled)
            {
                existing.Status = ReminderDeliveryStatus.Pending;
                existing.NextAttemptAtUtc = due;
            }
            existing.EventTitle = calendarEvent.Title;
            return;
        }

        ReminderDeliveryRecord delivery = new()
        {
            Id = Guid.NewGuid(), EventId = calendarEvent.Id,
            EventTitle = calendarEvent.Title, Channel = channel,
            DueAtUtc = due, NextAttemptAtUtc = due,
            Status = ReminderDeliveryStatus.Pending
        };
        dbContext.ReminderDeliveries.Add(delivery);
        known.Add(key, delivery);
    }

    private static bool IsScheduled(CalendarEvent calendarEvent, ReminderDeliveryRecord delivery)
    {
        if (!calendarEvent.IsEnabled || calendarEvent.IsDeleted) return false;
        if (calendarEvent.RecurrenceKind == EventRecurrenceKind.None)
            return calendarEvent.GetReminderSchedule().Any(item =>
                item.Channel == delivery.Channel && item.TriggerAtUtc == delivery.DueAtUtc);

        if (delivery.Channel != ReminderChannel.Desktop) return false;
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(calendarEvent.TimeZoneId);
        DateOnly date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(delivery.DueAtUtc, zone).Date);
        return calendarEvent.GetRecurringNotificationTimes(date).Contains(delivery.DueAtUtc);
    }
}
