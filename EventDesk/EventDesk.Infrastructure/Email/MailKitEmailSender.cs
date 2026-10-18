using EventDesk.Application.Email;
using EventDesk.Application.Reminders;
using EventDesk.Constants;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EventDesk.Infrastructure.Email;

public sealed class MailKitEmailSender(IEmailSettingsStore settingsStore) : IEmailSender
{
    public async Task SendReminderAsync(ReminderClaim claim, CancellationToken cancellationToken)
    {
        EmailSettings settings = await settingsStore.GetAsync(cancellationToken);
        if (!settings.IsEnabled)
            throw new EmailDeliveryException(EmailConstants.NotConfigured, true);

        EmailSettingsValidator.Validate(settings);
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse(settings.FromAddress));
        foreach (string address in settings.RecipientAddresses)
            message.Bcc.Add(MailboxAddress.Parse(address));
        message.Subject = claim.EventTitle.Replace('\r', ' ').Replace('\n', ' ');
        message.Body = new TextPart(EmailConstants.PlainTextSubtype)
        {
            Text = string.IsNullOrWhiteSpace(claim.EventDescription)
                ? claim.EventTitle : claim.EventDescription
        };
        message.MessageId = $"{claim.DeliveryId:N}@{EmailConstants.MessageIdDomain}";

        using SmtpClient client = new();
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port,
                settings.UseImplicitTls ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);
            await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            try
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception)
            {
                // SMTP already accepted the message; a disconnect failure must not trigger a resend.
            }
        }
        catch (AuthenticationException)
        {
            throw new EmailDeliveryException(EmailConstants.AuthenticationFailed, true);
        }
        catch (SmtpCommandException exception)
        {
            bool permanent = (int)exception.StatusCode >= 500;
            throw new EmailDeliveryException(EmailConstants.Rejected, permanent);
        }
        catch (SmtpProtocolException)
        {
            throw new EmailDeliveryException(EmailConstants.ConnectionFailed, false);
        }
        catch (System.Security.Authentication.AuthenticationException)
        {
            throw new EmailDeliveryException(EmailConstants.ConnectionFailed, false);
        }
        catch (IOException)
        {
            throw new EmailDeliveryException(EmailConstants.ConnectionFailed, false);
        }
        catch (System.Net.Sockets.SocketException)
        {
            throw new EmailDeliveryException(EmailConstants.ConnectionFailed, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new EmailDeliveryException(EmailConstants.SendFailed, false);
        }
    }
}
