using System;

namespace QueueKiosk.Services;

public class NotificationMockService
{
    public List<string> Logs { get; } = new();

    public void SendNotification(string ticketNumber, string message)
    {
        // Mocking an external API call for SMS/WhatsApp
        var log = $"[{DateTime.Now:HH:mm:ss}] Notification sent for {ticketNumber}: {message}";
        Logs.Insert(0, log);
        if (Logs.Count > 50) Logs.RemoveAt(Logs.Count - 1);
        Console.WriteLine(log);
    }
}
