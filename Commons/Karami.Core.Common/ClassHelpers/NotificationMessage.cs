namespace Karami.Core.Common.ClassHelpers;

public class NotificationMessage
{
    public string ConnectionId { get; set; } //SignalR
    public Payload Payload     { get; set; }
}

public class Payload
{
    public int Code       { get; set; }
    public string Message { get; set; }
    public object Body    { get; set; }
}