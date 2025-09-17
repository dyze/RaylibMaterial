using NLog;

namespace Editor;



class Program
{
    public static void Main()
    {
        var messageQueueTarget = new MessageQueueTarget();

        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(NLog.LogLevel.Trace).WriteToConsole();
            builder.ForLogger().Targets.Add(messageQueueTarget);
        });

        //Logger.Info("Main");

        var app = new EditorController();
        messageQueueTarget.MessageQueue = EditorController.MessageQueue;
        
        app.Run();

        
    }
}