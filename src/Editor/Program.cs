using NLog;

namespace Editor;


class Program
{
    public static void Main(string[] args)
    {
        var messageQueueTarget = new MessageQueueTarget();

        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(NLog.LogLevel.Trace).WriteToConsole();
            builder.ForLogger().Targets.Add(messageQueueTarget);
        });

        string filePath = null;
        if(args.Length > 0)
            filePath = args[0];

        var app = new EditorController(filePath);
        messageQueueTarget.MessageQueue = EditorController.MessageQueue;
        
        app.Run();
    }
}