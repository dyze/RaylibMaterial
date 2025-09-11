using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using static System.Net.Mime.MediaTypeNames;
using Color = Raylib_cs.Color;

namespace Editor;

class App
{
    private const string ShaderFolderPath = "resources\\shaders";

    private const int ScreenWidth = 1280; // initial size of window
    private const int ScreenHeight = 720;

    //private int _windowWidthBeforeFullscreen = ScreenWidth;
    //private int _windowHeightBeforeFullscreen = ScreenWidth;
    //private bool _windowSizeChanged; // set to true when switching to fullscreen

    //private readonly List<Size> _displayResolutions =
    //[
    //    new(320, 180), // 0
    //    new(640, 36), // 1
    //    new(1280, 720), // 2
    //    new(1600, 900), // 3
    //    new(1920, 1080) // 4
    //];

    //private int _currentDisplayResolutionIndex = 2;



    private Camera3D _camera;

    private Model _currentModel;
    private Shader _shader;
    private Shader _defaultMaterialShader;
    private string? _currentShaderName;

    private Dictionary<string, ShaderInfo> _shaders = new Dictionary<string, ShaderInfo>();

    private Dictionary<string, ShaderCode> _shaderCode = new Dictionary<string, ShaderCode>();


    public void Run()
    {


        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Terrain Erosion (.NET)");
        rlImGui.Setup();

        RetrieveShaders();

        _currentModel = GenerateCubeModel();


        _defaultMaterialShader = Raylib.LoadShader($"{ShaderFolderPath}\\base.vert", $"{ShaderFolderPath}\\base.frag");

        _currentShaderName = "base";
        SelectShader(_currentShaderName);


        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on


        SetCamera();

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateCamera(ref _camera, CameraMode.Orbital);

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);


            Raylib.DrawFPS(10, 10);

            Raylib.BeginMode3D(_camera);


            Raylib.DrawModel(_currentModel,
                new Vector3(0f, 0.0f, 0f),
            1.0f,
            Color.Red);

            Raylib.DrawGrid(10, 1.0f);


            Raylib.EndMode3D();

            RenderShaderList();
            RenderShaderCode();

            rlImGui.End();
            Raylib.EndDrawing();
        }

        rlImGui.Shutdown();
    }

    private void RenderShaderCode()
    {
        if (ImGui.Begin("Code"))
        {
            ImGuiTabBarFlags flags = ImGuiTabBarFlags.None;
            if (ImGui.BeginTabBar("MyTabBar", flags))
            {
                foreach (var (key, code) in _shaderCode)
                {
                    var name = key;
                    if (code.Modified)
                        name += " *";
                    if (ImGui.BeginTabItem(name))
                    {
                        //ImGui.Text(code.Code);
                        ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.AllowTabInput;
                        if (ImGui.InputTextMultiline("##source",
                                ref code.Code,
                                20000,
                                new Vector2(-1, -1),//ImGui.GetTextLineHeight() * 16),
                                inputFlags))
                        {
                            code.Modified = true;
                        }

                        if (code.Modified)
                            if (ImGui.Button("Apply"))
                            {
                                var valid = LoadShader(_currentShaderName);

                                code.IsValid = valid;

                            }

                        if(code.IsValid == false)
                            ImGui.TextColored(new Vector4(1f, 0, 0, 1f),
                                "not valid");

                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }
            ImGui.Separator();


            ImGui.End();
        }

    }


    private void RenderShaderList()
    {
        if (ImGui.Begin("Shaders"))
        {
            string[] items = new string[_shaders.Count];
            int item_current = -1;
            for (int i = 0; i < _shaders.Count; i++)
            {
                items[i] = _shaders.Keys.ElementAt(i);
                if (_currentShaderName == items[i])
                    item_current = i;
            }


            if (ImGui.ListBox("Shaders", ref item_current, items, items.Length))
            {
                _currentShaderName = items[item_current];
                SelectShader(_currentShaderName);
            }

            if (ImGui.Button("refresh"))
            {
                RetrieveShaders();
            }

            ImGui.End();
        }


    }

    private void RetrieveShaders()
    {
        _shaders = [];

        var files = Directory.EnumerateFiles(ShaderFolderPath, "*.*", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            var name = Path.GetFileNameWithoutExtension(fi.Name);

            if (_shaders.ContainsKey(name) == false)
            {
                string? vertexShaderFileName = null;
                string? fragmentShaderFileName = null;

                var vertexShaderFilePath = $"{ShaderFolderPath}\\{name}.vert";
                var fragmentShaderFilePath = $"{ShaderFolderPath}\\{name}.frag";

                if (File.Exists(vertexShaderFilePath))
                    vertexShaderFileName = $"{name}.vert";
                if (File.Exists(fragmentShaderFilePath))
                    fragmentShaderFileName = $"{name}.frag";

                if (vertexShaderFileName == null &&
                    fragmentShaderFileName == null)
                    throw new ApplicationException("both files are null");

                var shaderInfo = new ShaderInfo(vertexShaderFileName,
                    fragmentShaderFileName);
                _shaders.Add(name, shaderInfo);
            }
        }
    }

    private void ApplyShader()
    {
        Raylib.SetMaterialShader(ref _currentModel, 0, ref _shader);
    }

    private void SelectShader(string shaderName)
    {
        var item = _shaders[shaderName];

        LoadCode(item);

        LoadShader(shaderName);
    }

    private bool LoadShader(string shaderName)
    {
        var item = _shaders[shaderName];

        _shader = Raylib.LoadShaderFromMemory(
            item.VertexShaderFileName != null ? _shaderCode[item.VertexShaderFileName].Code : null,
            item.FragmentShaderFileName != null ? _shaderCode[item.FragmentShaderFileName].Code : null);

        bool valid = Raylib.IsShaderValid(_shader);

        if (valid == false)
            _shader = _defaultMaterialShader;

        ApplyShader();

        return valid;
    }

    private void LoadCode(ShaderInfo shaderInfo)
    {
        _shaderCode = new Dictionary<string, ShaderCode>();

        if (shaderInfo.VertexShaderFileName != null)
        {
            _shaderCode.Add(shaderInfo.VertexShaderFileName, new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{shaderInfo.VertexShaderFileName}")));
        }
        if (shaderInfo.FragmentShaderFileName != null)
        {
            _shaderCode.Add(shaderInfo.FragmentShaderFileName, new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{shaderInfo.FragmentShaderFileName}")));
        }
    }

    private Model GenerateCubeModel()
    {
        Mesh mesh = Raylib.GenMeshCube(2, 2, 2);
        Model model = Raylib.LoadModelFromMesh(mesh);
        return model;
    }

    private void SetCamera()
    {
        // Define our custom camera to look into our 3d world
        _camera = new Camera3D(new Vector3(12.0f, 15, 22.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            45.0f,
            CameraProjection.Perspective);
    }
}