using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Services;

/// <summary>
/// Defines operations for sending notifications related to studio communications,
/// including SMS messages, email messages, payment confirmations,
/// invoice due reminders, and studio closure alerts.
/// </summary>
public interface INotificationService
{
    Task SendSmsAsync(string toPhoneNumber, string messageBody);

    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody);

    Task SendPaymentConfirmationAsync(
        string toEmail,
        string toPhone,
        string parentName,
        decimal amountPaid);

    Task SendInvoiceDueReminderAsync(
        string toEmail,
        string toPhone,
        string parentName,
        decimal balanceDue,
        DateTime dueDate);

    Task SendStudioClosureAlertAsync(
        List<ParentProfile> parents,
        string closureReason,
        DateTime closureDate);
}