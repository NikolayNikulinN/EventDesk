using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EventDesk.App.Services;
using EventDesk.Constants;
using EventDesk.Contracts.Reminders;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Resources.Abstract;

namespace EventDesk.App.ViewModels.Pages;

public sealed class HistoryViewModel(
    IReminderHistoryClient client, IResourceWrapper resources,
    ILoggerWrapper logger) : ObservableObject
{
    private string message = string.Empty;
    private bool isLoading;

    public string Title => resources.History;
    public ObservableCollection<ReminderHistoryRow> Rows { get; } = [];

    public string Message
    {
        get => message;
        private set => SetProperty(ref message, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (isLoading) return;
        isLoading = true;
        Message = resources.HistoryLoading;
        try
        {
            IReadOnlyList<ReminderHistoryEntry> entries = await client.GetAsync(cancellationToken);
            Rows.Clear();
            foreach (ReminderHistoryEntry entry in entries)
            {
                string channel = entry.Channel switch
                {
                    ReminderHistoryChannelCode.Desktop => resources.HistoryChannelDesktop,
                    ReminderHistoryChannelCode.Email => resources.HistoryChannelEmail,
                    _ => entry.Channel.ToString()
                };
                string status = entry.Status switch
                {
                    ReminderHistoryStatusCode.Pending => resources.HistoryStatusPending,
                    ReminderHistoryStatusCode.Delivering => resources.HistoryStatusDelivering,
                    ReminderHistoryStatusCode.Delivered => resources.HistoryStatusDelivered,
                    ReminderHistoryStatusCode.Failed => resources.HistoryStatusFailed,
                    ReminderHistoryStatusCode.Canceled => resources.HistoryStatusCanceled,
                    _ => entry.Status.ToString()
                };
                Rows.Add(new ReminderHistoryRow(entry.EventTitle,
                    string.Format(CultureInfo.CurrentCulture, resources.HistoryDueFormat,
                        entry.DueAtUtc.ToLocalTime()),
                    string.Format(CultureInfo.CurrentCulture, resources.HistoryAttemptsFormat,
                        entry.AttemptCount), channel, status));
            }
            Message = Rows.Count == 0 ? resources.HistoryEmpty : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Warning, ReminderProcessingConstants.HistoryLoadFailed);
            Message = resources.HistoryLoadFailed;
        }
        finally
        {
            isLoading = false;
        }
    }
}

public sealed record ReminderHistoryRow(
    string EventTitle, string DueLabel, string AttemptsLabel,
    string ChannelLabel, string StatusLabel);
