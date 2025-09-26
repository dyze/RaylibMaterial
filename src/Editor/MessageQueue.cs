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
    public  LogLevel logLevel { get; }
    public  string text { get; }

    public Message(LogLevel logLevel,
        string text)
    {
        this.logLevel = logLevel;
        this.text = text;
    }
}

public class MessageQueue
{
    public static int NbMessages = 50;
    private static readonly Queue<Message> _messages = new();

    public void Queue(Message message)
    {
        if (_messages.Count >= NbMessages)
            _messages.Dequeue();

        //var concat = string.Format("{0}{1}", message, message2);

        _messages.Enqueue(message);
    }

    public Message[] GetMessages()
    {
        return _messages.ToArray();
    }

    public void Clear()
    {
        _messages.Clear();
    }
}