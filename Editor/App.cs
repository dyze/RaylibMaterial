using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace Editor;

class App
{
    private class ToolConfig
    {
        public string Name { get; set; }
        public string ImageFileName;
        public Image Image;
        public Texture2D Texture;

        public ToolConfig(string name,
            string imageFileName)
        {
            ImageFileName = imageFileName;
            Name = name;
        }
    }

    private const string ShaderFolderPath = "resources\\shaders";

    private Vector2 ScreenSize = new(1280, 720); // initial size of window

    private Vector2 OutputSize = new(1280 / 2, 720 / 2);


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
    private float CameraXAngle = 0f;
    private float CameraYAngle = 0f;
    private float Distance = 7f;

    private Model _currentModel;
    private Shader _shader;
    private Shader _defaultMaterialShader;
    private string? _currentShaderName;

    private Dictionary<string, ShaderInfo> _shaders = new();

    private Dictionary<string, ShaderCode> _shaderCode = new();

    private RenderTexture2D _viewTexture;

    enum ModelType
    {
        Cube = 0,
        Plane = 1,
        Sphere = 2,
    }

    private ModelType _modelType = ModelType.Cube;

    private readonly Dictionary<ModelType, ToolConfig> _configs = new()
    {
        { ModelType.Cube, new ToolConfig("cube", "cube.png") },
        { ModelType.Plane, new ToolConfig("plane", "plane.png") },
        { ModelType.Sphere, new ToolConfig("sphere", "sphere.png") },
    };

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)ScreenSize.X, (int)ScreenSize.Y, "Terrain Erosion (.NET)");
        rlImGui.Setup();

        PrepareUi();

        _viewTexture = Raylib.LoadRenderTexture((int)OutputSize.X, (int)OutputSize.Y);

        RetrieveShaders();

        SelectModelType();



        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on


        SetCamera();

        var prevMousePos = Raylib.GetMousePosition();

        while (!Raylib.WindowShouldClose())
        {
            prevMousePos = HandleCamera(prevMousePos);

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            Raylib.BeginTextureMode(_viewTexture);


            Raylib.BeginMode3D(_camera);

            Raylib.ClearBackground(Color.Black);

            Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);

            //Raylib.DrawGrid(10, 1.0f);


            Raylib.EndMode3D();

            Raylib.DrawFPS(10, 10);

            Raylib.EndTextureMode();

            RenderToolBar();
            RenderShaderList();
            RenderShaderCode();
            RenderVariables();
            RenderOutput();

            rlImGui.End();
            Raylib.EndDrawing();
        }

        rlImGui.Shutdown();
    }

    private Vector2 HandleCamera(Vector2 prevMousePos)
    {
        var mouseDelta = Raylib.GetMouseWheelMove();

        var newDistance = Distance + mouseDelta * 0.01f;
        if (newDistance <= 0)
            newDistance = 0.01f;

        Distance = newDistance;


        var thisPos = Raylib.GetMousePosition();

        var delta = Raymath.Vector2Subtract(prevMousePos, thisPos);
        prevMousePos = thisPos;

        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            CameraXAngle += delta.X / 100;
            CameraYAngle += delta.Y / 100;

            //_camera.Position = Raymath.Vector3RotateByAxisAngle(_camera.Position, Vector3.UnitY, delta.X/100);
            //_camera.Position = Raymath.Vector3RotateByAxisAngle(_camera.Position, Vector3.UnitX, delta.Y / 100);
        }

        _camera.Position = new Vector3((float)(Math.Cos(CameraXAngle) * Distance),
            (float)(Math.Sin(CameraYAngle) * Distance),
                Distance);

        return prevMousePos;
    }

    private void SelectModelType()
    {
        switch (_modelType)
        {
            case ModelType.Cube:
                _currentModel = GenerateCubeModel();
                break;
            case ModelType.Plane:
                _currentModel = GeneratePlaneModel();
                break;
            case ModelType.Sphere:
                _currentModel = GenerateSphereModel();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        _defaultMaterialShader = Raylib.LoadShader($"{ShaderFolderPath}\\base.vert", $"{ShaderFolderPath}\\base.frag");

        _currentShaderName = "base";
        SelectShader(_currentShaderName);
    }



    private void PrepareUi()
    {
        foreach (var (key, tool) in _configs)
        {
            tool.Image = Raylib.LoadImage($"resources/ui/{tool.ImageFileName}");
            tool.Texture = Raylib.LoadTextureFromImage(tool.Image);
        }
    }

    private void RenderToolBar()
    {
        ImGui.SetNextWindowSize(new Vector2(200, 80));
        if (ImGui.Begin("Tools", ImGuiWindowFlags.NoTitleBar))
        {
            foreach (var (key, tool) in _configs)
            {
                ImGui.SameLine();
                if (rlImGui.ImageButtonSize(tool.Name,
                        tool.Texture,
                        new Vector2(32, 32)))
                {
                    _modelType = key;
                    SelectModelType();
                }
            }

            ImGui.End();
        }
    }

    private void RenderOutput()
    {
        ImGui.SetNextWindowSize(OutputSize);
        if (ImGui.Begin("Output", ImGuiWindowFlags.NoResize))
        {
            rlImGui.ImageRenderTexture(_viewTexture);
            ImGui.End();
        }
    }

    private void RenderShaderCode()
    {
        if (ImGui.Begin("Code"))
        {
            var flags = ImGuiTabBarFlags.None;
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
                        var inputFlags = ImGuiInputTextFlags.AllowTabInput;
                        if (ImGui.InputTextMultiline("##source",
                                ref code.Code,
                                20000,
                                new Vector2(-1, -1), //ImGui.GetTextLineHeight() * 16),
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

                        if (code.IsValid == false)
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
            var item_current = -1;
            for (var i = 0; i < _shaders.Count; i++)
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

    private void RenderVariables()
    {
        if (ImGui.Begin("Variables"))
        {
            foreach (var (key, code) in _shaderCode)
            {
                foreach (var variable in code.Variables)
                {
                    ImGui.LabelText(variable.Name, variable.Type.ToString());
                }
            }

            ImGui.End();
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
            var code = new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{shaderInfo.VertexShaderFileName}"));
            code.ParseVariables();
            _shaderCode.Add(shaderInfo.VertexShaderFileName, code);
        }

        if (shaderInfo.FragmentShaderFileName != null)
        {
            var code = new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{shaderInfo.FragmentShaderFileName}"));
            code.ParseVariables();
            _shaderCode.Add(shaderInfo.FragmentShaderFileName, code);
        }
    }

    private Model GenerateCubeModel()
    {
        var mesh = Raylib.GenMeshCube(2, 2, 2);
        var model = Raylib.LoadModelFromMesh(mesh);
        return model;
    }

    private Model GeneratePlaneModel()
    {
        var mesh = Raylib.GenMeshPlane(2, 2, 1, 1);
        var model = Raylib.LoadModelFromMesh(mesh);
        return model;
    }

    private Model GenerateSphereModel()
    {
        var mesh = Raylib.GenMeshSphere(2, 10, 10);
        var model = Raylib.LoadModelFromMesh(mesh);
        return model;
    }

    private void SetCamera()
    {
        // Define our custom camera to look into our 3d world
        _camera = new Camera3D(new Vector3(6.0f, 6, 6),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            45f,
            CameraProjection.Perspective);
    }
}