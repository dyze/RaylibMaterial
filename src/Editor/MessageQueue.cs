namespace Editor;

public enum LogLevel
{
    Trace = 0,
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
}

public class Message
{
    public  LogLevel LogLevel { get; }
    public  string Text { get; }

    public Message(LogLevel logLevel,
        string text)
    {
        this.LogLevel = logLevel;
        this.Text = text;
    }
}

public class MessageQueue
{
    public static int NbMessages = 50;
    private static readonly Queue<Message> Messages = new();

    public void Queue(Message message)
    {
        if (Messages.Count >= NbMessages)
            Messages.Dequeue();

        //var concat = string.Format("{0}{1}", message, message2);

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