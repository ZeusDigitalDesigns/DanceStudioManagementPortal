using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Services;

/// <summary>
/// Provides notification delivery services for the Eatonville Dance Studio application.
/// </summary>
/// <remarks>
/// Supports sending SMS messages, email messages, payment confirmations, invoice due reminders,
/// and studio closure alerts. SMS delivery is performed through Twilio when the required
/// configuration values are available, with fallback logging behavior when notification
/// credentials are not configured.
/// </remarks>
public class NotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<NotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task SendSmsAsync(
        string toPhoneNumber,
        string messageBody)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            return;
        }

        string? accountSid =
            _config["Notifications:Twilio:AccountSid"];
        string? authToken =
            _config["Notifications:Twilio:AuthToken"];
        string? fromPhone =
            _config["Notifications:Twilio:FromPhone"];

        // Fallback to console log if keys are not configured yet
        if (string.IsNullOrWhiteSpace(accountSid) ||
            string.IsNullOrWhiteSpace(authToken) ||
            string.IsNullOrWhiteSpace(fromPhone))
        {
            _logger.LogInformation(
                "[SMS SIMULATED] To: {Phone} | Body: {Body}",
                toPhoneNumber,
                messageBody);
            return;
        }

        string url =
            $"https://api.twilio.com/2010-04-01/Accounts/" +
            $"{accountSid}/Messages.json";

        HttpClient client = _httpClientFactory.CreateClient();
        byte[] authBytes = Encoding.ASCII.GetBytes(
            $"{accountSid}:{authToken}");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(authBytes));

        Dictionary<string, string> formData =
            new Dictionary<string, string>
            {
                { "To", toPhoneNumber },
                { "From", fromPhone },
                { "Body", messageBody }
            };

        FormUrlEncodedContent content =
            new FormUrlEncodedContent(formData);

        await client.PostAsync(url, content);
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        string? apiKey = _config["Notifications:SendGrid:ApiKey"];
        string fromEmail =
            _config["Notifications:SendGrid:FromEmail"]
            ?? "noreply@eatonvilledance.com";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation(
                "[EMAIL SIMULATED] To: {Email} | Subject: {Subj}",
                toEmail,
                subject);
            return;
        }

        HttpClient client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        object payload = new
        {
            personalizations = new[]
            {
                new { to = new[] { new { email = toEmail } } }
            },
            from = new
            {
                email = fromEmail,
                name = "Eatonville Dance Studio"
            },
            subject = subject,
            content = new[]
            {
                new { type = "text/html", value = htmlBody }
            }
        };

        string json = JsonSerializer.Serialize(payload);
        StringContent content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        await client.PostAsync(
            "https://api.sendgrid.com/v3/mail/send",
            content);
    }

    public async Task SendPaymentConfirmationAsync(
        string toEmail,
        string toPhone,
        string parentName,
        decimal amountPaid)
    {
        string formattedAmount = amountPaid.ToString("C");
        string smsText =
            $"Eatonville Dance Studio: Hi {parentName}, your " +
            $"payment of {formattedAmount} was received. Thank you!";

        string emailHtml =
            $"<h3>Payment Confirmation</h3>" +
            $"<p>Hi {parentName},</p>" +
            $"<p>We received your payment of " +
            $"<strong>{formattedAmount}</strong>.</p>";

        await SendSmsAsync(toPhone, smsText);
        await SendEmailAsync(
            toEmail,
            "Payment Confirmation - Eatonville Dance Studio",
            emailHtml);
    }

    public async Task SendInvoiceDueReminderAsync(
        string toEmail,
        string toPhone,
        string parentName,
        decimal balanceDue,
        DateTime dueDate)
    {
        string formattedBalance = balanceDue.ToString("C");
        string dateText = dueDate.ToString("MMM dd, yyyy");

        string smsText =
            $"Eatonville Dance Studio: Hi {parentName}, your " +
            $"tuition balance of {formattedBalance} is due on " +
            $"{dateText}. Log in to Family Hub to pay.";

        string emailHtml =
            $"<h3>Upcoming Invoice Due Reminder</h3>" +
            $"<p>Hi {parentName},</p>" +
            $"<p>Your current studio balance of " +
            $"<strong>{formattedBalance}</strong> is due by " +
            $"<strong>{dateText}</strong>.</p>";

        await SendSmsAsync(toPhone, smsText);
        await SendEmailAsync(
            toEmail,
            "Tuition Reminder - Eatonville Dance Studio",
            emailHtml);
    }

    public async Task SendStudioClosureAlertAsync(
        List<ParentProfile> parents,
        string closureReason,
        DateTime closureDate)
    {
        string dateStr = closureDate.ToString("dddd, MMM dd");

        foreach (ParentProfile parent in parents)
        {
            string smsText =
                $"URGENT STUDIO CLOSURE ({dateStr}): " +
                $"Eatonville Dance Studio is closed. " +
                $"Reason: {closureReason}";

            string emailHtml =
                $"<h3 style='color:#ff10f0;'>Studio Closure Alert</h3>" +
                $"<p>Eatonville Dance Studio will be closed on " +
                $"<strong>{dateStr}</strong>.</p>" +
                $"<p><strong>Reason:</strong> {closureReason}</p>";

            await SendSmsAsync(parent.PrimaryPhone, smsText);

            if (parent.User != null &&
                !string.IsNullOrEmpty(parent.User.Email))
            {
                await SendEmailAsync(
                    parent.User.Email,
                    $"URGENT: Studio Closure ({dateStr})",
                    emailHtml);
            }
        }
    }
}