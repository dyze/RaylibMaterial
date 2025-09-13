using System.Numerics;
using ImGuiNET;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace Editor;

class App
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string ResourceUiPath = "resources/ui";
    private const string ShaderFolderPath = "resources\\shaders";

    private readonly Vector2 _screenSize = new(1280, 720); // initial size of window

    private readonly Vector2 _outputSize = new(1280 / 2, 720 / 2);

    private Camera3D _camera;
    private float _modelXAngle = (float)(Math.PI / 4);
    private float _modelYAngle = (float)(Math.PI / 4);
    private float _distance = 0f;

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

    enum BackgroundType
    {
        None = 0,
        Cloud = 1,
        WildPark = 2,
    }

    private BackgroundType _backgroundType = BackgroundType.None;

    private readonly Dictionary<BackgroundType, BackgroundConfig> _backgrounds = new()
    {
        { BackgroundType.None, new BackgroundConfig("none", null) },
        { BackgroundType.Cloud, new BackgroundConfig("clouds", "clouds.jpg") },
        { BackgroundType.WildPark, new BackgroundConfig("wild park", "wildpark.png") },
    };

    private readonly MessageWindow _messageWindow = new();
    private bool _messageWindowIsVisible = true;
    public static MessageQueue? MessageQueue { get; set; } = new();

    // private Model _backgroundModel;

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)_screenSize.X, (int)_screenSize.Y, "Raylib Material Editor");
        rlImGui.Setup();

        PrepareUi();

        _viewTexture = Raylib.LoadRenderTexture((int)_outputSize.X, (int)_outputSize.Y);

        RetrieveShaders();

        SelectModelType();

        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on

        PrepareCamera();

        var prevMousePos = Raylib.GetMousePosition();

        Logger.Info("all is set");

        while (!Raylib.WindowShouldClose())
        {
            prevMousePos = HandleCamera(prevMousePos);

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            RenderOutput();

            RenderToolBar();
            RenderShaderList();
            RenderShaderCode();
            RenderVariables();
            RenderOutputWindow();

            _messageWindow.Render(MessageQueue, ref _messageWindowIsVisible);

            rlImGui.End();
            Raylib.EndDrawing();
        }

        rlImGui.Shutdown();
    }

    private void RenderOutput()
    {
        Raylib.BeginTextureMode(_viewTexture);

        Raylib.BeginMode3D(_camera);
        Raylib.ClearBackground(Color.Black);

        if (_backgroundType != BackgroundType.None)
        {
            var background = _backgrounds[_backgroundType];
            Raylib.DrawBillboard(_camera, background.Texture, new Vector3(0f, 0f, 2f), 10f, Color.White);
        }

        // Raylib.DrawPlane(new Vector3(0f, 0f, 1f), new Vector2(10f, 10f), Color.Blue);


        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);


        Raylib.EndMode3D();

        Raylib.DrawFPS(10, 10);

        Raylib.EndTextureMode();
    }

    private Vector2 HandleCamera(Vector2 prevMousePos)
    {
        var mouseDelta = Raylib.GetMouseWheelMove();

        var newDistance = _distance + mouseDelta * 0.01f;
        if (newDistance <= 0)
            newDistance = 0.01f;

        _distance = newDistance;


        var thisPos = Raylib.GetMousePosition();

        var delta = Raymath.Vector2Subtract(prevMousePos, thisPos);
        prevMousePos = thisPos;

        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            _modelXAngle += delta.X / 100;
            _modelYAngle += delta.Y / 100;
        }

        _currentModel.Transform = Raymath.MatrixRotateXYZ(new Vector3(_modelXAngle, _modelYAngle, 0));
        _currentModel.Transform.Translation = new Vector3(0, 0, _distance);


        //_camera.Position = new Vector3((float)(Math.Cos(_modelXAngle) * _distance),
        //    (float)(Math.Sin(_modelYAngle) * _distance),
        //    _distance);

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
        foreach (var (_, tool) in _configs)
        {
            tool.Image = Raylib.LoadImage($"{ResourceUiPath}/{tool.ImageFileName}");
            tool.Texture = Raylib.LoadTextureFromImage(tool.Image);
        }

        foreach (var (_, background) in _backgrounds)
        {
            if (background.ImageFileName == null)
                continue;
            background.Image = Raylib.LoadImage($"{ResourceUiPath}/{background.ImageFileName}");
            background.Texture = Raylib.LoadTextureFromImage(background.Image);
        }
    }

    private void RenderToolBar()
    {
        ImGui.SetNextWindowSize(new Vector2(400, 80));
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
                    Logger.Trace($"{_modelType} selected");
                    SelectModelType();
                }
            }

            ImGui.SameLine(40);

            foreach (var (key, background) in _backgrounds)
            {
                ImGui.SameLine();
                if (rlImGui.ImageButtonSize(background.Name,
                        background.Texture,
                        new Vector2(32, 32)))
                {
                    _backgroundType = key;
                    Logger.Trace($"{_backgroundType} selected");
                    // SelectBackgroundType();
                }
            }

            ImGui.End();
        }
    }

    //private void SelectBackgroundType()
    //{
    //    backgroundModel = GenerateBackground();
    //}

    private void RenderOutputWindow()
    {
        ImGui.SetNextWindowSize(_outputSize);
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
            var itemCurrent = -1;
            for (var i = 0; i < _shaders.Count; i++)
            {
                items[i] = _shaders.Keys.ElementAt(i);
                if (_currentShaderName == items[i])
                    itemCurrent = i;
            }


            if (ImGui.ListBox("Shaders", ref itemCurrent, items, items.Length))
            {
                _currentShaderName = items[itemCurrent];
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
            foreach (var (_, code) in _shaderCode)
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
        Logger.Info($"Apply shader");

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
        Logger.Info($"Loading shader {shaderName}");

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

    //private Model GenerateBackground()
    //{
    //    var mesh = Raylib.GenMeshPlane(2, 2, 1, 1);
    //    var model = Raylib.LoadModelFromMesh(mesh);
    //    model.Transform = Raymath.MatrixTranslate(0f, 0f, 2f);
    //    return model;
    //}

    private Model GenerateSphereModel()
    {
        var mesh = Raylib.GenMeshSphere(2, 10, 10);
        var model = Raylib.LoadModelFromMesh(mesh);
        return model;
    }

    private void PrepareCamera()
    {
        // Define our custom camera to look into our 3d world
        _camera = new Camera3D(new Vector3(0f, 0f, -5),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            45f,
            CameraProjection.Perspective);
    }
}