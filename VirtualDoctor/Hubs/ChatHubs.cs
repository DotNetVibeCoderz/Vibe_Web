using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using VirtualDoctor.Services.AI;

namespace VirtualDoctor.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IAiChatService _chat;
    public ChatHub(IAiChatService chat) => _chat = chat;

    public async Task SendMessage(string chatId, string message, string? provider = null, string? imageUrl = null, string? documentUrl = null)
    {
        var uid = Context.UserIdentifier ?? Context.ConnectionId;
        await Clients.Caller.SendAsync("TypingIndicator", true);
        await foreach (var chunk in _chat.SendStreamingMessageAsync(uid, chatId, message, provider, imageUrl, documentUrl))
            await Clients.Caller.SendAsync("ReceiveChunk", chunk);
        await Clients.Caller.SendAsync("TypingIndicator", false);
        await Clients.Caller.SendAsync("MessageComplete");
    }
    public async Task JoinChatGroup(string chatId) => await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    public async Task LeaveChatGroup(string chatId) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
}

[Authorize]
public class ConsultationHub : Hub
{
    private readonly Services.IConsultationService _cs;
    public ConsultationHub(Services.IConsultationService cs) => _cs = cs;

    public async Task JoinConsultation(string cid) => await Groups.AddToGroupAsync(Context.ConnectionId, cid);
    public async Task LeaveConsultation(string cid) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, cid);

    public async Task SendConsultationMessage(string cid, string sid, string sn, string msg)
    {
        await _cs.SendMessageAsync(cid, sid, sn, msg);
        await Clients.Group(cid).SendAsync("ReceiveMessage", new { SenderId = sid, SenderName = sn, Message = msg, SentAt = DateTime.UtcNow });
    }
    public async Task StartTyping(string cid, string name) => await Clients.OthersInGroup(cid).SendAsync("UserTyping", name);
    public async Task StopTyping(string cid) => await Clients.OthersInGroup(cid).SendAsync("UserStoppedTyping");
}
