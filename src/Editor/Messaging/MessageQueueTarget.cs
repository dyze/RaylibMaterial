using NLog;
using NLog.Targets;

namespace Editor.Messaging;


[Target("MessageQueue")]
public sealed class MessageQueueTarget(MessageQueue? messageQueue = null) : TargetWithLayout
{
    private readonly Dictionary<NLog.LogLevel, LogLevel> _logLevels = new()
    {
        { NLog.LogLevel.Debug, LogLevel.Debug },
        { NLog.LogLevel.Trace, LogLevel.Trace },
        { NLog.LogLevel.Info, LogLevel.Info },
        { NLog.LogLevel.Warn, LogLevel.Warning },
        { NLog.LogLevel.Error, LogLevel.Error },
        { NLog.LogLevel.Fatal, LogLevel.Fatal },
    };

    public MessageQueue? MessageQueue { get; set; } = messageQueue;

    protected override void Write(LogEventInfo logEvent)
    {
        var logMessage = Layout.Render(logEvent);

        var logLevel = _logLevels[logEvent.Level];

        MessageQueue?.Queue(new Message(logLevel, logMessage));
    }

}