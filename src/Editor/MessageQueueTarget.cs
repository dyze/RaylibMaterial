using NLog;
using NLog.Targets;

namespace Editor;


[Target("MessageQueue")]
public sealed class MessageQueueTarget : TargetWithLayout
{
    private Dictionary<NLog.LogLevel, LogLevel> _logLevels = new()
    {
        { NLog.LogLevel.Debug, LogLevel.Debug },
        { NLog.LogLevel.Trace, LogLevel.Trace },
        { NLog.LogLevel.Info, LogLevel.Info },
        { NLog.LogLevel.Warn, LogLevel.Warning },
        { NLog.LogLevel.Error, LogLevel.Error },
        { NLog.LogLevel.Fatal, LogLevel.Fatal },
    };

    public MessageQueueTarget(MessageQueue? messageQueue = null)
    {
        MessageQueue = messageQueue;
    }

    // [RequiredParameter]
    public MessageQueue? MessageQueue { get; set; }

    protected override void Write(LogEventInfo logEvent)
    {
        var logMessage = this.Layout.Render(logEvent);

        var logLevel = _logLevels[logEvent.Level];

        MessageQueue?.Queue(new Message(logLevel, logMessage));
    }

}