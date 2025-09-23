using Examples.Shared;
using ImGuiNET;
using Library.Packaging;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;

namespace ConsumerSampleApp;

internal class Controller
{
    private readonly Vector2 _screenSize = new(1600, 900);
    private Model _currentModel;
    private List<Light> lights = new();
    private Camera3D _camera;
    private FileInfo[] _files;
    private MaterialPackage _materialPackage;
    private Shader? _currentShader;
    /// <summary>
    /// This shader is used if we are not able to load a user one
    /// We proceed like that to prevent crash when trying to use a faulty user shader
    /// </summary>
    private Shader _defaultShader;
    private const string MaterialsPath = "materials";

    public Controller()
    {
    }

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

        _defaultShader = Raylib.LoadShader($"{MaterialsPath}\\base.vert", $"{MaterialsPath}\\base.frag");

        while (!Raylib.WindowShouldClose())
        {
            UpdateLights();

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            RenderModel();
            RenderUi();

            rlImGui.End();
            Raylib.EndDrawing();
        }

        rlImGui.Shutdown();
    }

    private void EnumerateMaterials()
    {
        var di = new DirectoryInfo(MaterialsPath);

        _files = di.GetFiles("*.mat", SearchOption.AllDirectories);
    }

    private void RenderUi()
    {
        ImGui.SetNextWindowSize(new Vector2(400, 80));
        if (ImGui.Begin("Materials"))
        {
            foreach (var file in _files)
            {
                ImGui.SameLine();
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
        _materialPackage = new MaterialPackage();
        _materialPackage.Load(filePath);



        var vertexShader = _materialPackage.GetShaderCode(FileType.VertexShader);
        var fragmentShader = _materialPackage.GetShaderCode(FileType.FragmentShader);

        if (_currentShader.HasValue
            && _currentShader.Value.Id != _defaultShader.Id)
            Raylib.UnloadShader(_currentShader.Value);


        _currentShader = Raylib.LoadShaderFromMemory(
            vertexShader != null ? System.Text.Encoding.UTF8.GetString(vertexShader.Value.Value) : null,
            fragmentShader != null ? System.Text.Encoding.UTF8.GetString(fragmentShader.Value.Value) : null);

        bool valid = Raylib.IsShaderValid(_currentShader.Value);

        if (valid == false)
            _currentShader = _defaultShader;

        Shader shader = _currentShader.Value;
        Raylib.SetMaterialShader(ref _currentModel, 0, ref shader);

        //CreateLights();
    }

    private void RenderModel()
    {
        Raylib.BeginMode3D(_camera);

        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);

        RenderLights();

        Raylib.EndMode3D();
    }

    public void RenderLights()
    {
        foreach (var light in lights)
        {
            Raylib.DrawSphereEx(light.Position, 0.2f, 8, 8, light.Color);
        }
    }

    private void UpdateLights()
    {
        foreach (var light in lights)
        {
            //Rlights.UpdateLightValues(_currentShader.Value, light);
        }
    }
}