using System;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class UserNotification
{
    private UserNotification()
    {
        Type = string.Empty;
        Title = string.Empty;
        Message = string.Empty;
        Payload = string.Empty;
    }

    public UserNotification(Guid userId, string type, string title, string message, string payload, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        Payload = payload;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Type { get; private set; }

    public string Title { get; private set; }

    public string Message { get; private set; }

    public string Payload { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public void MarkAsRead(DateTimeOffset readAtUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = readAtUtc;
    }
}
