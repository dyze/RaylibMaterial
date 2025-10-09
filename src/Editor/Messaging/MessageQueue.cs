namespace Editor.Messaging;

public enum LogLevel
{
    Trace = 0,
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
}

public class Message(
    LogLevel logLevel,
    string text)
{
    public  LogLevel LogLevel { get; } = logLevel;
    public  string Text { get; } = text;
}

public class MessageQueue
{
    public static int NbMessages = 50;
    private static readonly Queue<Message> Messages = new();

    public void Queue(Message message)
    {
        if (Messages.Count >= NbMessages)
            Messages.Dequeue();

        Messages.Enqueue(message);
    }

    public Message[] GetMessages()
    {
        return Messages.ToArray();
    }

    public void Clear()
    {
        Messages.Clear();
    }
}