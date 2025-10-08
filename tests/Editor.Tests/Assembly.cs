using NLog;

namespace Editor.Tests;

[TestClass]
public class Assembly
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [AssemblyInitialize]
    public static void TestInitialize(TestContext testContext)
    {
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(NLog.LogLevel.Trace).WriteToConsole();
        });

        Logger.Info("TestInitialize OK");
    }

    [AssemblyCleanup]
    public static void TearDown()
    {
        Logger.Info("TearDown OK");
    }
}