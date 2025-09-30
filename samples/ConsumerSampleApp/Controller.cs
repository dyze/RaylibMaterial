using ImGuiNET;
using Library.Packaging;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using Library.Lighting;

namespace ConsumerSampleApp;

internal class Controller
{
    private readonly Vector2 _screenSize = new(1600, 900);
    private Model _currentModel;
    private readonly List<Light> _lights = [];
    private Camera3D _camera;
    private FileInfo[] _files = [];
    private MaterialPackage? _materialPackage;


    private Shader? _currentShader;
    private const string MaterialsPath = "materials";

    internal void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                      ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)_screenSize.X, (int)_screenSize.Y, "Raylib MaterialMeta Editor");
        rlImGui.Setup();

        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on

        // Define our custom camera to look into our 3d world
        _camera = new Camera3D(new Vector3(5f, 5f, -5),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            45f,
            CameraProjection.Perspective);

        var mesh = Raylib.GenMeshCube(2, 2, 2);
        _currentModel = Raylib.LoadModelFromMesh(mesh);

        EnumerateMaterials();

        while (!Raylib.WindowShouldClose())
        {
            if (_currentShader.HasValue)
                UpdateLights();

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            RenderModels();

            RenderUi();

            rlImGui.End();
            Raylib.EndDrawing();
        }

        _materialPackage?.Dispose();
        rlImGui.Shutdown();
    }

    private void EnumerateMaterials()
    {
        var di = new DirectoryInfo(MaterialsPath);

        _files = di.GetFiles("*.mat", SearchOption.AllDirectories);
    }

    private void RenderUi()
    {
        ImGui.SetNextWindowSize(new Vector2(200, 400));
        if (ImGui.Begin("Materials"))
        {
            foreach (var file in _files)
            {
                if (ImGui.Button(file.Name))
                {
                    OnMaterialSelected(file.FullName);
                }
            }

            ImGui.End();
        }
    }

    private void OnMaterialSelected(string filePath)
    {
        _materialPackage = MaterialPackage.Load(filePath);

        var shader = _materialPackage.LoadShader();
        _currentShader = shader;
        Raylib.SetMaterialShader(ref _currentModel, 0, ref shader);

        _materialPackage.SendVariablesToModel(_currentModel);

        CreateLights();
    }

    private void RenderModels()
    {
        Raylib.BeginMode3D(_camera);

        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);
        RenderLights();

        Raylib.EndMode3D();
    }

    private void CreateLights()
    {
        if (_currentShader.HasValue == false)
            throw new NullReferenceException("_currentShader is null");

        LightManager.Clear();
        _lights.Clear();

        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(-2, 1, -2),
            Vector3.Zero,
            Color.Yellow,
            [_currentShader.Value]
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(2, 1, 2),
            Vector3.Zero,
            Color.Red,
            [_currentShader.Value]
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(-2, 1, 2),
            Vector3.Zero,
            Color.Green,
            [_currentShader.Value]
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(2, 1, -2),
            Vector3.Zero,
            Color.Blue,
            [_currentShader.Value]
        ));
    }

    public void RenderLights()
    {
        foreach (var light in _lights)
        {
            Raylib.DrawSphereEx(light.Position, 0.2f, 8, 8, light.Color);
        }
    }

    private void UpdateLights()
    {
        if (_currentShader.HasValue == false)
            throw new NullReferenceException("_currentShader is null");

        foreach (var light in _lights)
        {
            LightManager.UpdateLightValues(light);
        }
    }
}