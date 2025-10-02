using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;

namespace ConsumerSampleApp;

internal class Controller
{
    private readonly Vector2 _screenSize = new(1600, 900);

    private readonly List<Type> _examples = [typeof(OneModelOneMaterial), typeof(OneModelTwoMaterials)];     //TODO use reflection
    private ExampleBase? _currentExample;

    internal void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                      ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)_screenSize.X, (int)_screenSize.Y, "Raylib MaterialMeta Editor");
        rlImGui.Setup();

        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            rlImGui.Begin();

            _currentExample?.Run();

            RenderSampleUi();

            rlImGui.End();
            Raylib.EndDrawing();
        }

        _currentExample?.Close();
        _currentExample = null;

        rlImGui.Shutdown();
    }

    private void RenderSampleUi()
    {

        ImGui.SetNextWindowPos(new Vector2(200, 20), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.Always);
        if (ImGui.Begin("Welcome"))
        {
            ImGui.Text("""
                        This sample embeds several examples
                        Select below the one your want
                        """);

            foreach (var type in _examples)
            {
                if (ImGui.Button(type.ToString()))
                {
                    _currentExample?.Close();
                    _currentExample = null;

                    if (type.IsSubclassOf(typeof(ExampleBase)) == false)
                        throw new TypeAccessException($"{type} can't be used");

                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    var instance = ctor.Invoke(null);
                    if (instance == null)
                        throw new TypeAccessException($"{type} ctor failed");

                    _currentExample = instance as ExampleBase ?? throw new InvalidOperationException();
                    _currentExample.Init();


                    break;
                }
            }

            ImGui.End();
        }
    }


}