using Editor.Configuration;
using Editor.Helpers;
using Editor.Windows;
using Examples.Shared;
using ImGuiNET;
using Library;
using Library.CodeVariable;
using Library.Helpers;
using Library.Packaging;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using System.Collections;
using System.Numerics;
using Color = Raylib_cs.Color;

namespace Editor;

class EditorController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string ResourceUiPath = "resources/ui";
    private const string ShaderFolderPath = "resources/shaders";
    private const string MaterialsPath = "materials";

    private readonly Vector2 _screenSize = new(1600, 900); // initial size of window

    private readonly Vector2 _outputSize = new(1600 / 2, 900 / 2);

    private Camera3D _camera;
    private float _modelXAngle = (float)(Math.PI / 4);
    private float _modelYAngle = (float)(Math.PI / 4);
    private float _distance;

    private Model _currentModel;
    private Shader? _currentShader;

    /// <summary>
    /// This shader is used if we are not able to load a user one
    /// We proceed like that to prevent crash when trying to use a faulty user shader
    /// </summary>
    private Shader _defaultShader;

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
        Cloud = 0,
        WildPark = 1,
    }

    private readonly Dictionary<BackgroundType, BackgroundConfig> _backgrounds = new()
    {
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

    private List<Light> lights = new();

    public EditorController()
    {
        LoadEditorConfiguration();


        if (_editorConfiguration.DataFileExplorerConfiguration.DataFolderPath == null)
            _editorConfiguration.DataFileExplorerConfiguration.DataFolderPath = "./resources";

        _editorControllerData.DataFileExplorerData.DataFolder.Open(_editorConfiguration.DataFileExplorerConfiguration.DataFolderPath, AccessMode.Read);
        _editorControllerData.DataFileExplorerData.RefreshDataRootFolder();

        _dataFileExplorer = new(_editorConfiguration, _editorControllerData.DataFileExplorerData);

        _materialWindow = new(_editorControllerData);
        _materialWindow.OnSave += _materialWindow_OnSave;

        _shaderCodeWindow.ApplyChangesPressed += ShaderCodeWindowOnApplyChangesPressed;
    }

    private void _materialWindow_OnSave()
    {
        SaveMaterial();
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow((int)_screenSize.X, (int)_screenSize.Y, "Raylib MaterialMeta Editor");
        rlImGui.Setup();

        LoadUiResources();

        _defaultShader = Raylib.LoadShader($"{ShaderFolderPath}\\base.vert", $"{ShaderFolderPath}\\base.frag");

        _viewTexture = Raylib.LoadRenderTexture((int)_outputSize.X, (int)_outputSize.Y);

        var mesh = Raylib.GenMeshPlane(12, 8, 1, 1);
        _backgroundModel = Raylib.LoadModelFromMesh(mesh);

        var matRotate = Raymath.MatrixRotateXYZ(new Vector3((float)(-Math.PI / 2), (float)(Math.PI), 0));
        var matTranslate = Raymath.MatrixTranslate(0, 0, 3f);
        _backgroundModel.Transform = Raymath.MatrixMultiply(matRotate, matTranslate);


        SelectBackground(BackgroundType.WildPark);

        OnNewMaterial();


        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second
        Raylib.SetTraceLogLevel(TraceLogLevel.None); // disable logging from now on

        PrepareCamera();

        var prevMousePos = Raylib.GetMousePosition();

        Logger.Info("all is set");

        while (!Raylib.WindowShouldClose())
        {
            prevMousePos = HandleMouseMovement(prevMousePos);

            UpdateLights();

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            RenderMenu();
            Render();

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

        if (_currentShader.HasValue)
            Raylib.UnloadShader(_currentShader.Value);
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
                    OnNewMaterial();

                if (ImGui.MenuItem("Open"))
                    OnOpenMaterial();

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

    private void OnNewMaterial()
    {
        Logger.Info("OnNewMaterial...");

        _editorControllerData.MaterialPackage = new();
        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackage_OnFilesChanged;
        _editorControllerData.MaterialPackage.OnShaderChanged += MaterialPackage_OnShaderChanged;
        _editorControllerData.MaterialPackage.Meta.OnVariablesChanged += MaterialPackageMeta_OnVariablesChanged;

        _currentShader = _defaultShader;

        SelectModelType();
        LoadShaders();
    }

    private void OnOpenMaterial()
    {
        Logger.Info("OnOpenMaterial...");

        _editorControllerData.MaterialPackage = new();
        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackage_OnFilesChanged;
        _editorControllerData.MaterialPackage.OnShaderChanged += MaterialPackage_OnShaderChanged;
        _editorControllerData.MaterialPackage.Meta.OnVariablesChanged += MaterialPackageMeta_OnVariablesChanged;

        //TODO get name using file explorer or file drag
        var filePath = $"{MaterialsPath}/no name.mat";
        filePath = Path.GetFullPath(filePath);
        Logger.Info($"filePath={filePath}");

        _editorControllerData.MaterialPackage.Load(filePath);

        SelectModelType();
        LoadShaderCode();
        LoadShaders();
    }

    private void MaterialPackageMeta_OnVariablesChanged()
    {
        LoadShaderCode();
        ApplyVariables();
    }

    private void MaterialPackage_OnFilesChanged()
    {
    }

    private void MaterialPackage_OnShaderChanged()
    {
        LoadShaderCode();
        LoadShaders();
    }

    private void ShaderCodeWindowOnApplyChangesPressed(ShaderCode shaderCode)
    {
        shaderCode.IsValid = LoadShaders();
    }

    private void Render()
    {
        Raylib.BeginTextureMode(_viewTexture);

        Raylib.BeginMode3D(_camera);
        Raylib.ClearBackground(Color.Black);

        Raylib.DrawModel(_backgroundModel, Vector3.Zero, 1f, Color.White);
        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);

        RenderLights();

        Raylib.EndMode3D();

        Raylib.DrawFPS(10, 10);

        Raylib.EndTextureMode();
    }

    private Vector2 HandleMouseMovement(Vector2 prevMousePos)
    {
        var thisPos = Raylib.GetMousePosition();

        var mouseDelta = Raylib.GetMouseWheelMove();

        _distance = Math.Max(0f,
            _distance + mouseDelta * 0.1f);

        var delta = Raymath.Vector2Subtract(prevMousePos, Raylib.GetMousePosition());
        prevMousePos = thisPos;

        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            _modelYAngle -= delta.X / 100;
            _modelXAngle += delta.Y / 100;
        }

        var matRotate = Raymath.MatrixRotateXYZ(new Vector3(_modelXAngle, _modelYAngle, 0));
        var matTranslate = Raymath.MatrixTranslate(0, 0, _distance);
        _currentModel.Transform = Raymath.MatrixMultiply(matRotate, matTranslate);

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

        ApplyShaderToModel();
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
            var image = Raylib.LoadImage($"{ResourceUiPath}/{background.ImageFileName}");
            background.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
        }
    }

    private void SaveMaterial()
    {
        Logger.Info("SaveMaterial...");

        var filePath = $"{MaterialsPath}/{_editorControllerData.MaterialPackage.Meta.FileName}.mat";
        filePath = Path.GetFullPath(filePath);
        Logger.Info($"filePath={filePath}");

        _editorControllerData.MaterialPackage.Save(filePath);

        string argument = "/select, \"" + filePath + "\"";
        System.Diagnostics.Process.Start("explorer.exe", argument);

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
                    SelectBackground(key);
                }
            }

            ImGui.End();
        }
    }

    private void SelectBackground(BackgroundType key)
    {
        Logger.Trace($"{key} selected");
        var background = _backgrounds[key];
        Raylib.SetMaterialTexture(ref _backgroundModel, 0, MaterialMapIndex.Albedo, ref background.Texture);
    }

    private void RenderOutputWindow()
    {
        ImGui.SetNextWindowSize(_outputSize
            + new Vector2(0, ImGui.GetFrameHeightWithSpacing()));
        if (ImGui.Begin("Output", ImGuiWindowFlags.NoResize))
        {
            rlImGui.ImageRenderTexture(_viewTexture);
        }

        ImGui.End();
    }

    private void ApplyVariables()
    {
        Logger.Info("ApplyVariables...");

        if (_currentShader.HasValue == false)
            return;

        foreach (var (name, variable) in _editorControllerData.MaterialPackage.Meta.Variables)
        {
            var location = Raylib.GetShaderLocation(_currentShader.Value, name);
            if (location < 0)
            {
                Logger.Error($"location for {name} not found in shader. maybe because unused in code");
                continue;
            }

            if (variable.GetType() == typeof(CodeVariableVector4))
            {
                var currentValue = (variable as CodeVariableVector4).Value;
                Raylib.SetShaderValue(_currentShader.Value, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableColor))
            {
                var currentValue = TypeConvertors.ColorToVec4((variable as CodeVariableColor).Value);
                Raylib.SetShaderValue(_currentShader.Value, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableFloat))
            {
                var currentValue = (variable as CodeVariableFloat).Value;
                Raylib.SetShaderValue(_currentShader.Value, location, currentValue, ShaderUniformDataType.Float);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableTexture))
            {
                SetUniformTexture(name, (variable as CodeVariableTexture).Value);
            }
        }
    }

    private void SetUniformTexture(string variableName,
        string fileName)
    {
        Logger.Info("SetUniformTexture...");

        if(fileName == "")
        {
            Logger.Trace("No filename set");
            return;
        }

        var extension = Path.GetExtension(fileName);
        if (extension == null)
            throw new NullReferenceException($"No file extension found in {fileName}");

        var file = _editorControllerData.MaterialPackage.GetFile(FileType.Image, fileName);
        if (file == null)
            throw new NullReferenceException($"No file {fileName} found");

        var image = Raylib.LoadImageFromMemory(extension, file);       // ignore period
        if (Raylib.IsImageValid(image) == false)
        {
            Logger.Error($"image {fileName} is not valid");
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

        Logger.Trace($"{variableName}={fileName}");
    }

    private void ApplyShaderToModel()
    {
        Logger.Info("ApplyShader...");

        if (_currentShader.HasValue == false)
            return;

        Shader shader = _currentShader.Value;
        Raylib.SetMaterialShader(ref _currentModel, 0, ref shader);

        ApplyVariables();
    }

    private bool LoadShaders()
    {
        Logger.Info("LoadShaders...");

        var material = _editorControllerData.MaterialPackage;
        var vertexShader = material.GetShaderCode(FileType.VertexShader);
        var fragmentShader = material.GetShaderCode(FileType.FragmentShader);

        if (_currentShader.HasValue 
            && _currentShader.Value.Id != _defaultShader.Id)
            Raylib.UnloadShader(_currentShader.Value);

        _currentShader = Raylib.LoadShaderFromMemory(
            vertexShader != null ? System.Text.Encoding.UTF8.GetString(vertexShader.Value.Value) : null,
            fragmentShader != null ? System.Text.Encoding.UTF8.GetString(fragmentShader.Value.Value) : null);

        bool valid = Raylib.IsShaderValid(_currentShader.Value);

        if (valid == false)
            _currentShader = _defaultShader;

        Logger.Info($"shader id={_currentShader.Value.Id}");

        ApplyShaderToModel();

        CreateLights();

        return valid;
    }

    private void LoadShaderCode()
    {
        _shaderCode = new Dictionary<string, ShaderCode>();

        Dictionary<string, CodeVariableBase> allShaderVariables = [];

        var material = _editorControllerData.MaterialPackage;
        var shaderVariables = GetShaderCodeVariables(material, FileType.VertexShader);
        if (shaderVariables != null)
            allShaderVariables = allShaderVariables.Concat(shaderVariables).ToDictionary();

        shaderVariables = GetShaderCodeVariables(material, FileType.FragmentShader);
        if (shaderVariables != null)
            allShaderVariables = allShaderVariables.Concat(shaderVariables).ToDictionary();


        Logger.Info($"{allShaderVariables.Count} variables detected");


        material.ClearFileReferences();


        // Sync materialMeta variables
        foreach (var (key, variable) in allShaderVariables)
        {
            var result = material.Meta.Variables.TryGetValue(key, out var materialVariable);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in materialMeta -> create it");

                var newVariable = CodeVariableFactory.Build(variable.GetType());

                //TODO avoid trick
                if (variable.GetType() == typeof(CodeVariableColor))
                    // Set pink as default color
                    (newVariable as CodeVariableColor).Value = System.Drawing.Color.FromArgb(255, 255, 0, 255);

                material.Meta.Variables.Add(key, newVariable);
            }
            else
            {
                if (materialVariable == null)
                    throw new NullReferenceException("material variable is null");

                // already exist => check type change
                if (materialVariable.GetType() != variable.GetType())
                {
                    Logger.Trace($"{key}: type changed");
                    materialVariable = CodeVariableFactory.Build(variable.GetType());
                }
            }
        }

        List<string> toDelete = [];
        foreach (var (key, _) in material.Meta.Variables)
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
            material.Meta.Variables.Remove(name);
        }

        UpdateFileReferences();

        Logger.Trace($"{toDelete.Count} variables removed from materialMeta");
    }

    private void UpdateFileReferences()
    {
        var material = _editorControllerData.MaterialPackage;

        foreach (var (key, _) in material.Files)
        {
            material.CreateFileReferences(key);

            if (key.FileType == FileType.VertexShader ||
                key.FileType == FileType.FragmentShader)
            {
                if (material.GetShaderName(key.FileType) == key.FileName)
                    material.IncFileReferences(key);
            }
            else if (key.FileType == FileType.Image)
            {
                var count = material.Meta.Variables.Count(v =>
                                                v.Value.GetType() == typeof(CodeVariableTexture)
                                                && (v.Value as CodeVariableTexture).Value == key.FileName);
                material.IncFileReferences(key, (uint)count);
            }
        }
    }

    private Dictionary<string, CodeVariableBase>? GetShaderCodeVariables(MaterialPackage material,
        FileType shaderType)
    {
        var file = material.GetShaderCode(shaderType);
        if (file != null)
        {
            var fileName = file.Value.Key.FileName;
            var code = new ShaderCode(System.Text.Encoding.UTF8.GetString(file.Value.Value));
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
        var mesh = Raylib.GenMeshSphere(2, 20, 20);
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

    private void CreateLights()
    {
        lights.Clear();

        lights.Add(Rlights.CreateLight(
            0,
            LightType.Point,
            new Vector3(-2, 1, -2),
            Vector3.Zero,
            Color.Yellow,
            _currentShader.Value
        ));
        lights.Add(Rlights.CreateLight(
            1,
            LightType.Point,
            new Vector3(2, 1, 2),
            Vector3.Zero,
            Color.Red,
            _currentShader.Value
        ));
        lights.Add(Rlights.CreateLight(
            2,
            LightType.Point,
            new Vector3(-2, 1, 2),
            Vector3.Zero,
            Color.Green,
            _currentShader.Value
        ));
        lights.Add(Rlights.CreateLight(
            3,
            LightType.Point,
            new Vector3(2, 1, -2),
            Vector3.Zero,
            Color.Blue,
            _currentShader.Value
        ));
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
            Rlights.UpdateLightValues(_currentShader.Value, light);
        }
    }
}