using Microsoft.AspNetCore.SignalR;

namespace Lapak.Hubs;

/// <summary>
/// SignalR Hub for real-time AI chat (Tony Kurus & Siti Bohay)
/// </summary>
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Chat client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Chat client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific chat room (e.g., "tony-kurus-{userId}", "siti-bohay-{userId}")
    /// </summary>
    public async Task JoinChatRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        _logger.LogInformation("Client {ConnectionId} joined room {Room}", Context.ConnectionId, roomName);
    }

    /// <summary>
    /// Leave a chat room
    /// </summary>
    public async Task LeaveChatRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        _logger.LogInformation("Client {ConnectionId} left room {Room}", Context.ConnectionId, roomName);
    }

    /// <summary>
    /// Send a chat message to a specific room
    /// </summary>
    public async Task SendMessage(string roomName, string user, string message, string chatBotType)
    {
        await Clients.Group(roomName).SendAsync("ReceiveMessage", new
        {
            User = user,
            Message = message,
            ChatBotType = chatBotType,
            Timestamp = DateTime.UtcNow,
            IsBot = false
        });
    }

    /// <summary>
    /// Bot is typing indicator
    /// </summary>
    public async Task BotTyping(string roomName, bool isTyping)
    {
        await Clients.Group(roomName).SendAsync("BotTyping", isTyping);
    }

    /// <summary>
    /// Send streaming token
    /// </summary>
    public async Task SendStreamToken(string roomName, string token)
    {
        await Clients.Group(roomName).SendAsync("ReceiveToken", token);
    }
}

/// <summary>
/// SignalR Hub for real-time notifications
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Notification client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Subscribe to user-specific notifications
    /// </summary>
    public async Task SubscribeToUser(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    /// <summary>
    /// Send notification to specific user group
    /// </summary>
    public async Task SendNotification(string userId, string title, string message, string type)
    {
        await Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", new
        {
            Title = title,
            Message = message,
            Type = type, // info, success, warning, error
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// SignalR Hub for real-time dashboard updates
/// </summary>
public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Dashboard client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Join dashboard room for real-time updates
    /// </summary>
    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
        _logger.LogInformation("Client {ConnectionId} joined dashboard", Context.ConnectionId);
    }

    /// <summary>
    /// Broadcast dashboard data update
    /// </summary>
    public async Task UpdateDashboardData(object data)
    {
        await Clients.Group("dashboard").SendAsync("DashboardDataUpdated", data);
    }

    /// <summary>
    /// Broadcast order status update
    /// </summary>
    public async Task OrderStatusChanged(string orderNumber, string status)
    {
        await Clients.Group("dashboard").SendAsync("OrderStatusChanged", new
        {
            OrderNumber = orderNumber,
            Status = status,
            Timestamp = DateTime.UtcNow
        });
    }
}
