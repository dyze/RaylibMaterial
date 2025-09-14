using System.Numerics;
using Editor.Windows;
using ImGuiNET;
using Library;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace Editor;

class App
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string ResourceUiPath = "resources/ui";
    private const string ShaderFolderPath = "resources/shaders";
    private const string ImagesFolderPath = "resources/images";
    private const string MaterialsPath = "materials";


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

    private readonly Dictionary<BackgroundType, BackgroundConfig> _backgrounds = new()
    {
        { BackgroundType.None, new BackgroundConfig("none", null) },
        { BackgroundType.Cloud, new BackgroundConfig("clouds", "clouds.jpg") },
        { BackgroundType.WildPark, new BackgroundConfig("wild park", "wildpark.png") },
    };

    private readonly MessageWindow _messageWindow = new();
    private bool _messageWindowIsVisible = true;
    public static MessageQueue? MessageQueue { get; set; } = new();

    private Model _backgroundModel;

    private Library.Material _material = new();

    private ShaderCodeWindow _shaderCodeWindow = new();

    public void Run()
    {
        _shaderCodeWindow.ApplyChangesPressed += ShaderCodeWindowOnApplyChangesPressed;

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)_screenSize.X, (int)_screenSize.Y, "Raylib Material Editor");
        rlImGui.Setup();

        PrepareUi();

        _viewTexture = Raylib.LoadRenderTexture((int)_outputSize.X, (int)_outputSize.Y);

        var mesh = Raylib.GenMeshPlane(12, 8, 1, 1);
        _backgroundModel = Raylib.LoadModelFromMesh(mesh);

        var matRotate = Raymath.MatrixRotateXYZ(new Vector3((float)(-Math.PI / 2), 0, 0));
        var matTranslate = Raymath.MatrixTranslate(0, 0, 3f);
        _backgroundModel.Transform = Raymath.MatrixMultiply(matRotate, matTranslate);


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
            _shaderCodeWindow.Render(_shaderCode);
            if (VariablesWindow.Render(_material.Variables))
            {
                _material.SetModified();
                ApplyVariables();
            }

            RenderOutputWindow();
            RenderMaterial();

            _messageWindow.Render(MessageQueue,
                ref _messageWindowIsVisible);

            rlImGui.End();
            Raylib.EndDrawing();
        }

        Raylib.UnloadShader(_shader);
        rlImGui.Shutdown();
    }

    private void ShaderCodeWindowOnApplyChangesPressed(ShaderCode shaderCode)
    {
        var valid = LoadShader(_currentShaderName);

        shaderCode.IsValid = valid;
    }


    private void RenderOutput()
    {
        Raylib.BeginTextureMode(_viewTexture);

        Raylib.BeginMode3D(_camera);
        Raylib.ClearBackground(Color.Black);

        Raylib.DrawModel(_backgroundModel, Vector3.Zero, 1f, Color.White);

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
            _modelYAngle -= delta.X / 100;
            _modelXAngle += delta.Y / 100;
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

    private void RenderMaterial()
    {
        ImGui.SetNextWindowSize(new Vector2(100, 80), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Material"))
        {
            string fileName = _material.FileName;
            if (ImGui.InputText("FileName", ref fileName, 200))
            {
                _material.FileName = fileName;
                _material.SetModified();
            }

            ImGui.LabelText("FilePath", _material.FullFilePath);
            if (ImGui.InputText("Description", ref _material.Description, 200))
                _material.SetModified();
            if (ImGui.InputText("Author", ref _material.Author, 200))
                _material.SetModified();

            ImGui.BeginDisabled();
            var isModified = _material.IsModified;
            ImGui.Checkbox("is modified", ref isModified);
            ImGui.EndDisabled();
            ImGui.Separator();

            {
                ImGui.BeginDisabled(_material.IsModified == false);

                var saveMaterial = false;

                if (_material.IsModified)
                    ImGui.PushStyleColor(ImGuiCol.Button, TypeConvertors.ToVector4(System.Drawing.Color.Red));

                if (ImGui.Button("Save"))
                    saveMaterial = true;

                if (_material.IsModified)
                    ImGui.PopStyleColor(1);

                if (saveMaterial)
                    SaveMaterial();

                ImGui.EndDisabled();
            }
        }

        ImGui.End();
    }

    private void SaveMaterial()
    {
        Logger.Info("SaveMaterial...");

        var filePath = $"{MaterialsPath}/{_material.FileName}.mat";
        MaterialStorage.Save(_material, filePath);

        Logger.Info("SaveMaterial OK");
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
                    Logger.Trace($"{key} selected");
                    Raylib.SetMaterialTexture(ref _backgroundModel, 0, MaterialMapIndex.Albedo, ref background.Texture);

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
        }

        ImGui.End();
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
        }

        ImGui.End();
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


    private void ApplyVariables()
    {
        Logger.Info("ApplyVariables");


        foreach (var (name, variable) in _material.Variables)
        {
            var location = Raylib.GetShaderLocation(_shader, name);
            if (location < 0)
            {
                Logger.Error($"location for {name} not found in shader");
                continue;
            }

            if (variable.Type == typeof(Vector4))
            {
                var currentValue = (Vector4)variable.Value;
                Raylib.SetShaderValue(_shader, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.Type == typeof(float))
            {
                var currentValue = (float)variable.Value;
                Raylib.SetShaderValue(_shader, location, currentValue, ShaderUniformDataType.Float);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.Type == typeof(string))
            {
                SetUniformTexture(name, variable);
            }
        }
    }

    private void SetUniformTexture(string variableName, 
        CodeVariable variable)
    {
        var currentValue = (string)variable.Value;

        var imagePath = $"{ImagesFolderPath}/{currentValue}";
        var image = Raylib.LoadImage(imagePath);
        if (Raylib.IsImageValid(image) == false)
        {
            Logger.Error($"image {variableName} is not valid");
            return;
        }

        var texture = Raylib.LoadTextureFromImage(image);

        Raylib.UnloadImage(image);

        if (Raylib.IsTextureValid(texture) == false)
        {
            Logger.Error($"texture {variableName} is not valid");
            return;
        }

        Dictionary<string, MaterialMapIndex> table = new()
        {
            { "texture0", MaterialMapIndex.Albedo },
            { "texture1", MaterialMapIndex.Metalness },
            { "texture2", MaterialMapIndex.Normal },
            { "texture3", MaterialMapIndex.Roughness },
            { "texture4", MaterialMapIndex.Occlusion },
            { "texture5", MaterialMapIndex.Emission },
            { "texture6", MaterialMapIndex.Height },
            { "texture7", MaterialMapIndex.Cubemap },
            { "texture8", MaterialMapIndex.Irradiance },
            { "texture9", MaterialMapIndex.Prefilter },
            { "texture10", MaterialMapIndex.Brdf },
        };

        if (table.TryGetValue(variableName, out var index))
        {
            Raylib.SetMaterialTexture(ref _currentModel, 0, index, ref texture);
        }
        else
            Logger.Error($"texture index for {variableName} can't be found");

        Logger.Trace($"{variableName}={currentValue}");
    }

    private void ApplyShader()
    {
        Logger.Info("ApplyShader");

        Raylib.SetMaterialShader(ref _currentModel, 0, ref _shader);

        ApplyVariables();
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

        Raylib.UnloadShader(_shader);
        _shader = Raylib.LoadShaderFromMemory(
            item.VertexShaderFileName != null ? _shaderCode[item.VertexShaderFileName].Code : null,
            item.FragmentShaderFileName != null ? _shaderCode[item.FragmentShaderFileName].Code : null);

        bool valid = Raylib.IsShaderValid(_shader);

        if (valid == false)
            _shader = _defaultMaterialShader;

        Logger.Info($"shader id={_shader.Id}");

        ApplyShader();

        return valid;
    }

    private void LoadCode(ShaderInfo shaderInfo)
    {
        _shaderCode = new Dictionary<string, ShaderCode>();

        Dictionary<string, CodeVariable> shaderVariables = [];

        if (shaderInfo.VertexShaderFileName != null)
        {
            var code = new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{shaderInfo.VertexShaderFileName}"));
            code.ParseVariables();
            shaderVariables = code.Variables;
            _shaderCode.Add(shaderInfo.VertexShaderFileName, code);
        }

        if (shaderInfo.FragmentShaderFileName != null)
        {
            var code = new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{shaderInfo.FragmentShaderFileName}"));
            code.ParseVariables();
            shaderVariables = shaderVariables.Concat(code.Variables).ToDictionary(x => x.Key, x => x.Value);
            _shaderCode.Add(shaderInfo.FragmentShaderFileName, code);
        }

        Logger.Info($"{shaderVariables.Count} variables detected");

        // Sync material variables
        foreach (var (key, variable) in shaderVariables)
        {
            var result = _material.Variables.TryGetValue(key, out var materialVariable);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in material -> create it");
                _material.Variables.Add(key, new CodeVariable(variable.Type));
            }
            else
            {
                // exist check type
                if (materialVariable.Type != variable.Type)
                {
                    Logger.Trace($"{key}: type changed");
                    materialVariable.Type = variable.Type;
                }
            }
        }

        List<string> toDelete = [];
        foreach (var (key, _) in _material.Variables)
        {
            var result = shaderVariables.TryGetValue(key, out var _);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in code -> remove from material");
                toDelete.Add(key);
            }
        }

        foreach (var name in toDelete)
        {
            _material.Variables.Remove(name);
        }

        Logger.Trace($"{toDelete.Count} variables removed from material");
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