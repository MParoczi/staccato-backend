using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs;

public interface INotificationClient
{
    Task PdfReady(string exportId, string fileName);
    Task PdfFailed(string exportId, string errorCode);
}

[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
}