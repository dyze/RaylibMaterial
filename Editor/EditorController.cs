using System.Numerics;
using Editor.Configuration;
using Editor.Windows;
using ImGuiNET;
using Library;
using Library.Packaging;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace Editor;

class EditorController
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
    private float _distance;

    private Model _currentModel;
    private Shader _shader;

    /// <summary>
    /// This shader is used if we are not able to load a user one
    /// We proceed like that to prevent crash when trying to use a faulty user shader
    /// </summary>
    private readonly Shader _defaultMaterialShader;
    
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
    public static MessageQueue MessageQueue { get; set; } = new();

    private Model _backgroundModel;

    private readonly EditorControllerData _editorControllerData = new();


    private readonly ShaderCodeWindow _shaderCodeWindow = new();
    private EditorConfiguration _editorConfiguration = new();

    private readonly DataFileExplorer _dataFileExplorer;

    private readonly MaterialWindow _materialWindow;

    public EditorController()
    {
        LoadEditorConfiguration();

        _editorControllerData.DataFileExplorerData.DataFolder = new FileSystemAccess();

        if (_editorConfiguration.DataFileExplorerConfiguration.DataFolderPath == null)
            _editorConfiguration.DataFileExplorerConfiguration.DataFolderPath = "./resources";

        _editorControllerData.DataFileExplorerData.DataFolder.Open(_editorConfiguration.DataFileExplorerConfiguration.DataFolderPath, AccessMode.Read);
        _editorControllerData.DataFileExplorerData.RefreshDataRootFolder();

        _dataFileExplorer = new(_editorConfiguration, _editorControllerData.DataFileExplorerData);

        _materialWindow = new(_editorControllerData);

        _shaderCodeWindow.ApplyChangesPressed += ShaderCodeWindowOnApplyChangesPressed;

        _defaultMaterialShader = Raylib.LoadShader($"{ShaderFolderPath}\\base.vert", $"{ShaderFolderPath}\\base.frag");

        NewMaterial();
    }


    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)_screenSize.X, (int)_screenSize.Y, "Raylib MaterialMeta Editor");
        rlImGui.Setup();

        LoadUiResources();

        _viewTexture = Raylib.LoadRenderTexture((int)_outputSize.X, (int)_outputSize.Y);

        var mesh = Raylib.GenMeshPlane(12, 8, 1, 1);
        _backgroundModel = Raylib.LoadModelFromMesh(mesh);

        var matRotate = Raymath.MatrixRotateXYZ(new Vector3((float)(-Math.PI / 2), 0, 0));
        var matTranslate = Raymath.MatrixTranslate(0, 0, 3f);
        _backgroundModel.Transform = Raymath.MatrixMultiply(matRotate, matTranslate);

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

            RenderMenu();
            RenderOutput();

            RenderToolBar();

            _shaderCodeWindow.Render(_shaderCode);

            RenderOutputWindow();
            _materialWindow.Render();

            _messageWindow.Render(MessageQueue,
                ref _editorConfiguration.WorkspaceConfiguration.MessageWindowIsVisible);

            _dataFileExplorer.Render();

            rlImGui.End();
            Raylib.EndDrawing();
        }

        Raylib.UnloadShader(_shader);
        rlImGui.Shutdown();

        SaveEditorConfiguration();
    }

    private void RenderMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Package"))
            {
                if (ImGui.MenuItem("New"))
                    NewMaterial();

                if (ImGui.MenuItem("Save"))
                    SaveMaterial();

                //ImGui.Separator();

                //if (ImGui.MenuItem("Exit"))
                //    _engine.Exit(true);

                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Display"))
            {
                //if (ImGui.MenuItem("Fullscreen", null, _generalConfiguration.IsFullScreen))
                //    _generalConfiguration.IsFullScreen = !_generalConfiguration.IsFullScreen;

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                var workspace = _editorConfiguration.WorkspaceConfiguration;

                ImGuiHelpers.RenderCheckedMenuItem("Data file explorer", ref workspace.DataFileExplorerIsVisible);
                ImGuiHelpers.RenderCheckedMenuItem("Message window", ref workspace.MessageWindowIsVisible);

                ImGui.EndMenu();
            }
        }
        ImGui.EndMainMenuBar();
    }

    private void NewMaterial()
    {
        _editorControllerData.MaterialPackage = new();
        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackageOnOnFilesChanged;
        _editorControllerData.MaterialPackage.Meta.OnVariablesChanged += MetaOnOnVariablesChanged;
    }

    private void MetaOnOnVariablesChanged()
    {
        ApplyVariables();
    }

    private void MaterialPackageOnOnFilesChanged()
    {
        LoadShaderCode();
    }

    private void ShaderCodeWindowOnApplyChangesPressed(ShaderCode shaderCode)
    {
        var valid = LoadShaders();

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
    }

    private void LoadUiResources()
    {
        foreach (var (_, tool) in _configs)
        {
            var image = Raylib.LoadImage($"{ResourceUiPath}/{tool.ImageFileName}");
            tool.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
        }

        foreach (var (_, background) in _backgrounds)
        {
            if (background.ImageFileName == null)
                continue;
            var image = Raylib.LoadImage($"{ResourceUiPath}/{background.ImageFileName}");
            background.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
        }
    }

    private void SaveMaterial()
    {
        Logger.Info("SaveMaterial...");

        var filePath = $"{MaterialsPath}/{_editorControllerData.MaterialPackage.Meta.FileName}.mat";

        _editorControllerData.MaterialPackage.Save(filePath);

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
                }
            }

            ImGui.End();
        }
    }
    
    private void RenderOutputWindow()
    {
        ImGui.SetNextWindowSize(_outputSize);
        if (ImGui.Begin("Output", ImGuiWindowFlags.NoResize))
        {
            rlImGui.ImageRenderTexture(_viewTexture);
        }

        ImGui.End();
    }

    private void ApplyVariables()
    {
        Logger.Info("ApplyVariables");


        foreach (var (name, variable) in _editorControllerData.MaterialPackage.Meta.Variables)
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

    private bool LoadShaders()
    {
        Logger.Info("LoadShaders");

        var material = _editorControllerData.MaterialPackage;
        var vertexShaderFileName = material.GetFileOfType(FileType.VertexShader);
        var fragmentShaderFileName = material.GetFileOfType(FileType.FragmentShader);

        Raylib.UnloadShader(_shader);
        _shader = Raylib.LoadShaderFromMemory(
            vertexShaderFileName != null ? _shaderCode[vertexShaderFileName.Value.Key.FileName].Code : null,
            fragmentShaderFileName != null ? _shaderCode[fragmentShaderFileName.Value.Key.FileName].Code : null);

        bool valid = Raylib.IsShaderValid(_shader);

        if (valid == false)
            _shader = _defaultMaterialShader;

        Logger.Info($"shader id={_shader.Id}");

        ApplyShader();

        return valid;
    }

    private void LoadShaderCode()
    {
        _shaderCode = new Dictionary<string, ShaderCode>();

        Dictionary<string, CodeVariable> allShaderVariables = [];

        var material = _editorControllerData.MaterialPackage;
        var shaderVariables = GetShaderCodeForType(material, FileType.VertexShader);
        if (shaderVariables != null)
            allShaderVariables = allShaderVariables.Concat(shaderVariables).ToDictionary();

        shaderVariables = GetShaderCodeForType(material, FileType.FragmentShader);
        if (shaderVariables != null)
            allShaderVariables = allShaderVariables.Concat(shaderVariables).ToDictionary();


        Logger.Info($"{allShaderVariables.Count} variables detected");

        // Sync materialMeta variables
        foreach (var (key, variable) in allShaderVariables)
        {
            var result = _editorControllerData.MaterialPackage.Meta.Variables.TryGetValue(key, out var materialVariable);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in materialMeta -> create it");
                _editorControllerData.MaterialPackage.Meta.Variables.Add(key, new CodeVariable(variable.Type));
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
        foreach (var (key, _) in _editorControllerData.MaterialPackage.Meta.Variables)
        {
            var result = allShaderVariables.TryGetValue(key, out var _);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in code -> remove from materialMeta");
                toDelete.Add(key);
            }
        }

        foreach (var name in toDelete)
        {
            _editorControllerData.MaterialPackage.Meta.Variables.Remove(name);
        }

        Logger.Trace($"{toDelete.Count} variables removed from materialMeta");
    }

    private Dictionary<string, CodeVariable>? GetShaderCodeForType(MaterialPackage material, 
        FileType shaderType)
    {
        var file = material.GetFileOfType(shaderType);
        if (file != null)
        {
            var fileName = file.Value.Key.FileName;
            var code = new ShaderCode(
                File.ReadAllText($"{ShaderFolderPath}\\{fileName}"));
            code.ParseVariables();
            _shaderCode.Add(fileName, code);
            return code.Variables;
        }

        return null;
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

    private void LoadEditorConfiguration()
    {
        Logger.Info("Loading editor config...");

        try
        {
            _editorConfiguration = EditorConfigurationStorage.Load(".");
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);

            _editorConfiguration = new EditorConfiguration();
            return;
        }

        Logger.Info("editor config loaded");
    }

    private void SaveEditorConfiguration()
    {
        Logger.Info("Saving editor config...");

        try
        {
            EditorConfigurationStorage.Save(_editorConfiguration,
                ".");
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
            return;
        }

        Logger.Info("editor config saved");
    }
}