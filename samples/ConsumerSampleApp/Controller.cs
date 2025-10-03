using ImGuiNET;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using ConsumerSampleApp.Configuration;
using ConsumerSampleApp.Examples;
using System.Drawing;
using System.Reflection;
using Color = Raylib_cs.Color;

namespace ConsumerSampleApp;

internal class Controller
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<ExampleBase> _examples = [];
    private ExampleBase? _currentExample;

    private Configuration.Configuration _configuration = new();

    internal void Run()
    {
        LoadConfiguration();

        DiscoverExamples();

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                      ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow(_configuration.WindowSize.Width, _configuration.WindowSize.Height, "RaylibMaterial sample app");

        Raylib.SetWindowMonitor(_configuration.MonitorIndex);
        Raylib.SetWindowPosition(_configuration.WindowPosition.X, _configuration.WindowPosition.Y);

        Raylib.SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();

        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            _currentExample?.Run();

            RenderSampleUi();

            Raylib.DrawFPS(10, 10);

            rlImGui.End();
            Raylib.EndDrawing();
        }

        _currentExample?.Close();
        _currentExample = null;

        rlImGui.Shutdown();

        SaveConfiguration();
    }

    private void DiscoverExamples()
    { 
        var types = Assembly.GetAssembly(typeof(ExampleBase)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ExampleBase)));
        if (types == null)
            throw new TypeAccessException($"{nameof(types)} is null");

        Logger.Trace($"{types.Count()} examples found");

        foreach (var type in types)
        {
            // construct to get access to example name
            var ctor = type.GetConstructor([typeof(Configuration.Configuration)]);
            if (ctor == null)
                throw new TypeAccessException($"{type} ctor with this signature not found");
            var instance = ctor.Invoke([_configuration]);
            if (instance == null)
                throw new TypeAccessException($"{type} ctor failed");

            var obj = instance as ExampleBase ?? throw new InvalidOperationException();
            _examples.Add(obj);
        }
    }

    private void LoadConfiguration()
    {
        Logger.Info("Loading config...");

        try
        {
            _configuration = ConfigurationStorage.Load(".");
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);

            throw;
        }

        Logger.Info("editor config loaded");
    }

    private void SaveConfiguration()
    {
        Logger.Info("Saving config...");

        try
        {
            _configuration.MonitorIndex = Raylib.GetCurrentMonitor();
            var v = Raylib.GetWindowPosition();
            _configuration.WindowPosition = new Point((int)v.X, (int)v.Y);
            var width = Raylib.GetScreenWidth();
            var height = Raylib.GetScreenHeight();
            _configuration.WindowSize = new Size(width, height);

            ConfigurationStorage.Save(_configuration,
                ".");
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
            return;
        }

        Logger.Info("editor config saved");
    }

    private void RenderSampleUi()
    {
        ImGui.SetNextWindowPos(new Vector2(200, 20), ImGuiCond.Once);

        var screenWidth = Raylib.GetScreenWidth();
        var windowWidth = (int)screenWidth * 0.6f;

        ImGui.SetNextWindowPos(new Vector2((screenWidth - windowWidth)/2, 20), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(windowWidth, 200), ImGuiCond.Once);
        if (ImGui.Begin("Welcome"))
        {
            ImGui.Text("""
                        This sample embeds several examples
                        Select below the one your want
                        """);

            var i = 0;
            foreach (var example in _examples)
            {
                var type = example.GetType(); 

                if(i>0)
                    ImGui.SameLine();

                if (ImGui.Button(example.GetName()))
                {
                    Logger.Info($"{example.GetName()} selected");

                    _currentExample?.Close();
                    _currentExample = null;

                    var ctor = type.GetConstructor([typeof(Configuration.Configuration)]);
                    var instance = ctor.Invoke([_configuration]);
                    if (instance == null)
                        throw new TypeAccessException($"{type} ctor failed");

                    _currentExample = instance as ExampleBase ?? throw new InvalidOperationException();
                    _currentExample.Init();

                    break;
                }

                i++;
            }

            ImGui.Separator();

            ImGui.BeginChild("description");

            if(_currentExample == null)
                ImGui.Text("?");
            else
            {
                var summary = _currentExample.GetSummary();
                var description = _currentExample.GetDescription();

                var text = $"{summary}\n{description}\n";
                ImGui.Text(text);
            }

            ImGui.EndChild();
        }
        ImGui.End();
    }


}