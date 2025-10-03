using NLog;

namespace ConsumerSampleApp;

class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Main()
    {
        LogLevel minLogLevel = LogLevel.Info;

#if DEBUG
        minLogLevel = LogLevel.Trace;
#endif

        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(minLogLevel).WriteToConsole();
        });


        Logger.Info("Starting...");

        var controller = new Controller();

        controller.Run();
    }
}