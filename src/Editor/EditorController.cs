using System.Drawing;
using Editor.Configuration;
using Editor.Helpers;
using Editor.Windows;
using ImGuiNET;
using Library;
using Library.CodeVariable;
using Library.Packaging;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using Color = Raylib_cs.Color;
using System.Runtime.InteropServices;
using Library.Dialogs;
using Library.Lighting;

namespace Editor;

class EditorController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string MaterialsPath = "materials";
    private const string MaterialFileExtension = ".mat";
    private const string MaterialBackupFileExtension = ".mat.bck";

    private Camera3D _camera;


    private Model _currentModel;
    private Shader? _currentShader;

    /// <summary>
    /// This shader is used if we are not able to load a user one
    /// We proceed like that to prevent crash when trying to use a faulty user shader
    /// </summary>
    private Shader _defaultShader;

    private Dictionary<FileId, ShaderCode> _shaderCode = new();


    private readonly MessageWindow _messageWindow;
    public static MessageQueue MessageQueue { get; set; } = new();

    private SkyBox _skyBox = new();

    private readonly EditorControllerData _editorControllerData;


    private readonly CodeWindow _shaderCodeWindow;
    private EditorConfiguration _editorConfiguration = new();

    private readonly DataFileExplorer _dataFileExplorer;

    private readonly MaterialWindow _materialWindow;


    private FileDialogInfo? _fileDialogInfo;
    private string _outputFilePath;

    private MessageDialog.Configuration? _messageDialogConfiguration;
    private bool _processingRequestToClose;
    private bool _requestToCloseAccepted;
    private bool _requestToClose;
    private readonly string[] _supportedModelExtensions = [".obj", ".gltf", ".glb", ".vox", ".iqm", ".m3d"];
    private readonly string[] _supportedImagesExtensions = [".png", ".jpg"];

    private const string DefaultMaterialName = "new.mat";

    private string WindowCaption => $"Raylib Material Editor - {_outputFilePath}";

    private readonly OutputWindow _outputWindow;

    private bool _windowSizeChanged; // set to true when switching to fullscreen
    private Vector2 _previousMousePosition;

    private readonly SettingsWindow _settingsWindow;

    public EditorController()
    {
        LoadEditorConfiguration();

        if (_editorConfiguration.DataFileExplorerConfiguration.DataFolderPath == null)
            _editorConfiguration.DataFileExplorerConfiguration.DataFolderPath = "./resources";

        _editorControllerData = new(_editorConfiguration);

        _editorControllerData.DataFileExplorerData.DataFolder.Open(
            _editorConfiguration.DataFileExplorerConfiguration.DataFolderPath, AccessMode.Read);
        _editorControllerData.DataFileExplorerData.RefreshDataRootFolder();

        _messageWindow = new(_editorControllerData);


        _dataFileExplorer = new(_editorConfiguration, _editorControllerData,
            _editorControllerData.DataFileExplorerData);

        _shaderCodeWindow = new(_editorConfiguration,
            _editorControllerData);

        _materialWindow = new(_editorConfiguration,
            _editorControllerData);
        _materialWindow.OnSave += _materialWindow_OnSave;

        _shaderCodeWindow.BuildPressed += ShaderCodeWindow_OnBuildPressed;

        if (_editorConfiguration.OutputDirectoryPath == "")
            _editorConfiguration.OutputDirectoryPath = Path.GetFullPath($"{MaterialsPath}\\");

        _outputFilePath = _editorConfiguration.OutputDirectoryPath;
        Directory.CreateDirectory(_editorConfiguration.OutputDirectoryPath);

        DiscoverBuiltInModels();

        if (_editorConfiguration.CurrentModelFilePath == "" ||
            File.Exists(Path.GetFullPath(_editorConfiguration.CurrentModelFilePath)) == false)
        {
            _editorConfiguration.CurrentModelFilePath = _editorControllerData.BuiltInModels.First();
        }

        _outputWindow = new(_editorConfiguration,
            _editorControllerData);

        _outputWindow.ModelTypeChangeRequest += SelectModel;
        _outputWindow.BackgroundChanged += SelectBackground;
        _outputWindow.LightingPresetChangeRequest += CreateLights;
        _outputWindow.ResetCameraIsRequest += ResetCamera;

        _settingsWindow = new(_editorConfiguration);
        _settingsWindow.SavePressed += SaveEditorConfiguration;
    }
    
    private void ResetCamera()
    {
        _editorConfiguration.CameraSettings = new CameraSettings();
    }

    private void DiscoverBackgrounds()
    {
        var files = Directory.GetFiles(Path.GetFullPath(Resources.ResourceSkyBoxesFolderPath), "*.*",
                SearchOption.AllDirectories)
            .Where(file => _supportedImagesExtensions.Contains(Path.GetExtension(file)))
            .ToList();

        _editorControllerData.Backgrounds = new();

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var background = new BackgroundConfig(fileName, Path.GetFileName(filePath));
            var image = Raylib.LoadImage(filePath);
            background.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);

            _editorControllerData.Backgrounds.Add(fileName, background);
        }
    }

    private void DiscoverBuiltInModels()
    {
        _editorControllerData.BuiltInModels = Directory.GetFiles(Path.GetFullPath(Resources.ResourceModelsPath), "*.*",
                SearchOption.AllDirectories)
            .Where(file => _supportedModelExtensions.Contains(Path.GetExtension(file)))
            .ToList();
    }

    private void _materialWindow_OnSave()
    {
        OnSave();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static unsafe void CustomLog(int logLevel, sbyte* text, sbyte* args)
    {
        Dictionary<TraceLogLevel, NLog.LogLevel> levels = new()
        {
            { TraceLogLevel.All, NLog.LogLevel.Warn },
            { TraceLogLevel.Trace, NLog.LogLevel.Trace },
            { TraceLogLevel.Debug, NLog.LogLevel.Debug },
            { TraceLogLevel.Info, NLog.LogLevel.Info },
            { TraceLogLevel.Warning, NLog.LogLevel.Warn },
            { TraceLogLevel.Error, NLog.LogLevel.Error },
            { TraceLogLevel.Fatal, NLog.LogLevel.Fatal },
            { TraceLogLevel.None, NLog.LogLevel.Warn },
        };

        var level = levels.GetValueOrDefault((TraceLogLevel)logLevel, NLog.LogLevel.Warn);

        var message = Logging.GetLogMessage(new IntPtr(text), new IntPtr(args));

        Logger.Log(level, message);
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow(_editorConfiguration.WindowSize.Width, _editorConfiguration.WindowSize.Height, WindowCaption);

        Raylib.SetWindowMonitor(_editorConfiguration.MonitorIndex);
        Raylib.SetWindowPosition(_editorConfiguration.WindowPosition.X, _editorConfiguration.WindowPosition.Y);

        Raylib.SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();

        LoadUiResources();

        _defaultShader = Raylib.LoadShader($"{Resources.ResourceShaderFolderPath}\\base.vert",
            $"{Resources.ResourceShaderFolderPath}\\base.frag");

        _editorControllerData.ViewTexture = Raylib.LoadRenderTexture(400, 300);

        DiscoverBackgrounds();

        SelectBackground(_editorConfiguration.Background);

        NewMaterial();

        Raylib.SetTargetFPS(60); // Set our game to run at 60 frames-per-second

        PrepareCamera();

        unsafe
        {
            Raylib.SetTraceLogCallback(&CustomLog);
        }

        _previousMousePosition = Raylib.GetMousePosition();

        Logger.Info("all is set");

        while (true)
        {
            if (_requestToCloseAccepted)
                break;
            if (Raylib.WindowShouldClose() || _requestToClose)
            {
                _requestToClose = false;
                if (RequestCloseAccepted())
                    break;
            }

            HandleWindowResize();

            HandleMouseMovement();

            HandleFileDrop();

            UpdateLights();

            _editorControllerData.MaterialPackage.SetCameraPosition(_camera.Position);

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            RenderMenu();
            RenderModels();

            RenderFileDialog();
            RenderMessageDialog();
            _settingsWindow.Render();


            var codeIsModified = _shaderCodeWindow.Render(_shaderCode);
            if (codeIsModified)
            {
                foreach (var (key, value) in _shaderCode)
                {
                    var array = System.Text.Encoding.UTF8.GetBytes(value.Code);
                    _editorControllerData.MaterialPackage.UpdateFile(key, array);
                }

                _editorControllerData.MaterialPackage.SetModified();
            }

            _outputWindow.RenderOutputWindow();
            _materialWindow.Render();

            _messageWindow.Render(MessageQueue,
                ref _editorConfiguration.WorkspaceConfiguration.MessageWindowIsVisible);

            _dataFileExplorer.Render();

            rlImGui.End();
            Raylib.EndDrawing();

            if (_editorControllerData.WorkspaceLayoutResetRequested)
            {
                Logger.Trace("WorkspaceLayoutReset done");
                _editorControllerData.WorkspaceLayoutResetRequested = false;
            }
        }


        Raylib.UnloadShader(_defaultShader);

        _editorControllerData.MaterialPackage.Dispose();

        rlImGui.Shutdown();

        SaveEditorConfiguration();
    }

    private void HandleWindowResize()
    {
        if (Raylib.IsWindowResized() == false
            && _windowSizeChanged == false)
            return;

        _windowSizeChanged = false;
    }

    private void HandleFileDrop()
    {
        if (Raylib.IsFileDropped())
        {
            var droppedFiles = Raylib.GetDroppedFiles();
            if (droppedFiles.Length == 1)
            {
                var modelPath = droppedFiles.First();


                if (_supportedModelExtensions.Contains(Path.GetExtension(modelPath)))
                {
                    _editorConfiguration.CurrentModelFilePath = modelPath;
                    _editorConfiguration.CurrentModelType = EditorConfiguration.ModelType.Model;
                    SelectModel(modelPath);
                }
            }
        }
    }

    private void RenderMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Package"))
            {
                if (ImGui.MenuItem("New", "Ctrl+N"))
                    OnNewMaterial();

                if (ImGui.MenuItem("Load", "Ctrl+L"))
                    OnLoadMaterial(null);

                if (ImGui.BeginMenu("Load recent files"))
                {
                    if (_editorConfiguration.RecentFiles.Count == 0)
                        ImGui.MenuItem("empty", null, false, false);
                    else
                        foreach (var filePath in _editorConfiguration.RecentFiles)
                        {
                            if (ImGui.MenuItem(filePath))
                            {
                                OnLoadMaterial(filePath);
                                break;
                            }
                        }

                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("Save", "Ctrl+S"))
                    OnSave();

                if (ImGui.MenuItem("Save as"))
                    OnSaveAsStart();

                ImGui.Separator();

                if (ImGui.MenuItem("Exit"))
                    _requestToClose = true;

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Display"))
            {
                if (ImGui.MenuItem("Reset workspace layout"))
                    _editorControllerData.ResetWorkspaceLayout();
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

            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Options"))
                    _settingsWindow.Show();

                ImGui.EndMenu();
            }
        }

        ImGui.EndMainMenuBar();
    }

    private bool RequestCloseAccepted()
    {
        Logger.Info("RequestCloseAccepted...");

        if (_requestToCloseAccepted)
            return true;

        if (_processingRequestToClose)
            return false;


        if (_editorControllerData.MaterialPackage.IsModified)
        {
            _processingRequestToClose = true;
            _requestToCloseAccepted = false;

            _messageDialogConfiguration = new("Current material has not been saved",
                "Are you sure you want to continue?",
                [
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.Yes, "Yes, I'm sure",
                        _ => _requestToCloseAccepted = true,
                        System.Drawing.Color.Red),
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.No, "No, I changed my mind",
                        _ => _processingRequestToClose = false
                    )
                ]);
            return false;
        }

        return true;
    }

    private void OnNewMaterial()
    {
        Logger.Info("OnNewMaterial...");

        if (_editorControllerData.MaterialPackage.IsModified)
        {
            _messageDialogConfiguration = new("Current material has not been saved",
                "Are you sure you want to continue?",
                [
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.Yes, "Yes, I'm sure",
                        _ => NewMaterial(),
                        System.Drawing.Color.Red),
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.No, "No, I changed my mind"
                    )
                ]);
        }
        else
            NewMaterial();
    }

    private void NewMaterial()
    {
        _editorControllerData.MaterialFilePath = null;

        _editorControllerData.MaterialPackage = new();
        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackage_OnFilesChanged;
        _editorControllerData.MaterialPackage.OnShaderChanged += MaterialPackage_OnShaderChanged;
        _editorControllerData.MaterialPackage.OnVariablesChanged += MaterialPackageMeta_OnVariablesChanged;

        _currentShader = _defaultShader;

        _outputFilePath = $"{_editorConfiguration.OutputDirectoryPath}\\{DefaultMaterialName}";
        Raylib.SetWindowTitle(WindowCaption);

        LoadCurrentModel();
        LoadShaders();
    }

    private void OnLoadMaterial(string? filePath)
    {
        Logger.Info("OnLoadMaterial...");

        if (_editorControllerData.MaterialPackage.IsModified)
        {
            _messageDialogConfiguration = new("Current material has not been saved",
                "Are you sure you want to continue?",
                [
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.Yes, "Yes, I'm sure",
                        _ =>
                        {
                            if (filePath == null)
                                LoadMaterialAskForFile();
                            else
                                LoadMaterial(filePath);
                        },
                        System.Drawing.Color.Red),
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.No, "No, I changed my mind"
                    )
                ]);
        }
        else
        {
            if (filePath == null)
                LoadMaterialAskForFile();
            else
                LoadMaterial(filePath);
        }
    }

    private void LoadMaterialAskForFile()
    {
        Logger.Info("LoadMaterialAskForFile...");

        _fileDialogInfo = new()
        {
            Title = "Please select a material",
            Type = ImGuiFileDialogType.OpenFile,
            DirectoryPath = new DirectoryInfo(Path.GetDirectoryName(_outputFilePath)),
            FileName = "",
            Extensions =
            [
                new Tuple<string, string>("*" + MaterialFileExtension, "Materials"),
                new Tuple<string, string>("*" + MaterialBackupFileExtension, "Material backups")
            ]
        };

        Logger.Info("LoadMaterialAskForFile OK");
    }

    private void LoadMaterial(string filePath)
    {
        Logger.Info("LoadMaterial...");
        Logger.Info($"filePath={filePath}");

        try
        {
            _editorControllerData.MaterialPackage = MaterialPackage.Load(filePath);
        }
        catch (FileNotFoundException e)
        {
            Logger.Error(e);
            return;
        }

        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackage_OnFilesChanged;
        _editorControllerData.MaterialPackage.OnShaderChanged += MaterialPackage_OnShaderChanged;
        _editorControllerData.MaterialPackage.OnVariablesChanged += MaterialPackageMeta_OnVariablesChanged;

        _editorControllerData.MaterialFilePath = filePath;

        _outputFilePath = filePath;
        Raylib.SetWindowTitle(WindowCaption);

        _editorConfiguration.AddRecentFile(filePath);

        LoadCurrentModel();
        LoadShaderCode();
        LoadShaders();
    }

    private void MaterialPackageMeta_OnVariablesChanged()
    {
        _editorControllerData.MaterialPackage.UpdateFileReferences();

        LoadShaderCode();
        _editorControllerData.MaterialPackage.ApplyVariablesToModel(_currentModel);
    }

    private void MaterialPackage_OnFilesChanged()
    {
    }

    private void MaterialPackage_OnShaderChanged()
    {
        LoadShaderCode();
        LoadShaders();
    }

    private void ShaderCodeWindow_OnBuildPressed()
    {
        LoadShaders();
    }

    private void RenderModels()
    {
        Raylib.BeginTextureMode(_editorControllerData.ViewTexture);

        Raylib.BeginMode3D(_camera);
        Raylib.ClearBackground(Color.Black);

        Rlgl.DisableBackfaceCulling();
        Rlgl.DisableDepthMask();
        Raylib.DrawModel(_skyBox.Model, Vector3.Zero, 1f, Color.White);
        Rlgl.EnableBackfaceCulling();
        Rlgl.EnableDepthMask();

        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);

        if (_outputWindow.IsInDebugMode)
        {
            RenderLights();

            Raylib.DrawGrid(10, 1.0f);
        }

        Raylib.EndMode3D();

        Raylib.DrawFPS(10, 10);

        Raylib.EndTextureMode();
    }

    private void HandleMouseMovement()
    {
        var currentPosition = Raylib.GetMousePosition();
        var mouseDelta = Raylib.GetMouseWheelMove();

        var cameraSettings = _editorConfiguration.CameraSettings;

        if (_outputWindow.IsWindowHovered)
        {
            cameraSettings.Distance = Math.Max(CameraSettings.MinDistance,
                cameraSettings.Distance + mouseDelta * 0.1f);

            var delta = Raymath.Vector2Subtract(_previousMousePosition, currentPosition);

            if (Raylib.IsMouseButtonDown(MouseButton.Middle))
            {
                cameraSettings.Target.Y += delta.Y / 100;
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                cameraSettings.Angles.X -= delta.Y / 100;
                cameraSettings.Angles.Y += delta.X / 100;
            }
        }

        var q = Raymath.QuaternionFromEuler(cameraSettings.Angles.X, cameraSettings.Angles.Y, cameraSettings.Angles.Z);
        var v = Raymath.Vector3RotateByQuaternion(new Vector3(0, 0, -cameraSettings.Distance), q);

        _camera.Target = cameraSettings.Target;
        _camera.Position = v;

        _previousMousePosition = currentPosition;
    }

    private void SelectModel(EditorConfiguration.ModelType modelType, string modelFilePath)
    {
        Logger.Trace($"{modelType}, {modelFilePath} selected");
        _editorConfiguration.CurrentModelType = modelType;
        _editorConfiguration.CurrentModelFilePath = modelFilePath;
        LoadCurrentModel();
    }

    private void LoadCurrentModel()
    {
        switch (_editorConfiguration.CurrentModelType)
        {
            case EditorConfiguration.ModelType.Cube:
                _currentModel = GenerateCubeModel();
                break;
            case EditorConfiguration.ModelType.Plane:
                _currentModel = GeneratePlaneModel();
                break;
            case EditorConfiguration.ModelType.Sphere:
                _currentModel = GenerateSphereModel();
                break;
            case EditorConfiguration.ModelType.Model:
                SelectModel(_editorConfiguration.CurrentModelFilePath);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Logger.Trace($"MeshCount={_currentModel.MeshCount}, MaterialCount={_currentModel.MaterialCount}");

        ApplyShaderToModel();
    }

    private void SelectModel(string modelFilePath)
    {
        if (modelFilePath == "" ||
            File.Exists(Path.GetFullPath(modelFilePath)) == false)
        {
            modelFilePath = _editorControllerData.BuiltInModels.First();
        }

        Logger.Trace($"Loading {modelFilePath}");
        var model = Raylib.LoadModel(modelFilePath);

        if (Raylib.IsModelValid(model) == false)
            throw new InvalidDataException("model is not valid");

        _currentModel = model;

        _editorConfiguration.CurrentModelFilePath = modelFilePath;

        _editorConfiguration.AddCustomModel(modelFilePath);
    }

    private void LoadUiResources()
    {
        foreach (var (_, tool) in _editorControllerData.Tools)
        {
            var image = Raylib.LoadImage($"{Resources.ResourceToolBoarFolderPath}/{tool.ImageFileName}");
            tool.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
        }
    }

    private void OnSave()
    {
        Logger.Info("OnSave...");

        if (_editorControllerData.MaterialFilePath == null)
        {
            OnSaveAsStart();
            return;
        }

        _editorControllerData.MaterialPackage.Save(_editorControllerData.MaterialFilePath);
        _editorConfiguration.AddRecentFile(_editorControllerData.MaterialFilePath);

        //var argument = "/select, \"" + _editorControllerData.MaterialFilePath + "\"";
        //System.Diagnostics.Process.Start("explorer.exe", argument);

        Logger.Info("OnSave OK");
    }

    private void OnSaveAsStart()
    {
        Logger.Info("OnSaveAsStart...");

        _fileDialogInfo = new()
        {
            Title = "Please select a material",
            Type = ImGuiFileDialogType.SaveFile,
            DirectoryPath = new DirectoryInfo(Path.GetDirectoryName(_outputFilePath)),
            FileName = Path.GetFileName(_outputFilePath),
            Extensions =
            [
                new Tuple<string, string>("*" + MaterialFileExtension, "Materials")
            ]
        };

        Logger.Info("OnSaveAsStart OK");
    }

    private void SaveAs(string filePath)
    {
        Logger.Info("SaveAs...");

        _editorControllerData.MaterialFilePath = filePath;

        _editorControllerData.MaterialPackage.Save(_editorControllerData.MaterialFilePath);
        _editorConfiguration.AddRecentFile(_editorControllerData.MaterialFilePath);

        _outputFilePath = filePath;
        Raylib.SetWindowTitle(WindowCaption);

        var argument = "/select, \"" + _editorControllerData.MaterialFilePath + "\"";
        System.Diagnostics.Process.Start("explorer.exe", argument);

        Logger.Info("SaveAs OK");
    }

    private void RenderFileDialog()
    {
        var open = _fileDialogInfo != null;
        if (FileDialog.Run(ref open, _fileDialogInfo))
        {
            if (_fileDialogInfo.Type == ImGuiFileDialogType.OpenFile)
                LoadMaterial(_fileDialogInfo.ResultPath);
            else
            {
                if (File.Exists(_fileDialogInfo.ResultPath))
                {
                    var filePath = _fileDialogInfo.ResultPath;

                    _messageDialogConfiguration = new("A material with same name already exists",
                        "Are you sure you want to continue?",
                        [
                            new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.Yes, "Yes, I'm sure",
                                _ => SaveAs(filePath),
                                System.Drawing.Color.Red),
                            new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.No, "No, I changed my mind"
                            )
                        ]);
                }
                else
                    SaveAs(_fileDialogInfo.ResultPath);
            }
        }

        if (open == false)
            _fileDialogInfo = null;
    }

    private void RenderMessageDialog()
    {
        var buttonPressed = MessageDialog.Run(_messageDialogConfiguration);

        if (buttonPressed != null)
            _messageDialogConfiguration = null;

        if (buttonPressed != null)
        {
            Logger.Trace($"{buttonPressed.Id} has been pressed");

            buttonPressed.OnPressed?.Invoke(buttonPressed);
        }
    }

    private void SelectBackground(string? name)
    {
        Logger.Trace($"{name} selected");

        if (name == null || _editorControllerData.Backgrounds.TryGetValue(name, out var value) == false)
        {
            name = _editorControllerData.Backgrounds.Keys.First();
        }

        _editorConfiguration.Background = name;
        var background = _editorControllerData.Backgrounds[name];

        _skyBox = new SkyBox();

        var filePath = Path.GetFullPath($"{Resources.ResourceSkyBoxesFolderPath}/{background.ImageFileName}");
        _skyBox.GenerateModel(filePath);
    }

    private void ApplyShaderToModel()
    {
        Logger.Info("ApplyShader...");

        if (_currentShader.HasValue == false)
            return;

        var shader = _currentShader.Value;

        for (int i = 0; i < _currentModel.MaterialCount; i++)
        {
            Raylib.SetMaterialShader(ref _currentModel, i, ref shader);
        }

        _editorControllerData.MaterialPackage.ApplyVariablesToModel(_currentModel);
    }

    private void LoadShaders()
    {
        Logger.Info("LoadShaders...");

        var material = _editorControllerData.MaterialPackage;

        material.UnloadShader();

        var shaderIsValid = false;

        try
        {
            _currentShader = material.LoadShader();
            shaderIsValid = true;
            Logger.Info($"shader id={_currentShader.Value.Id}");
        }
        catch (InvalidDataException e)
        {
            Logger.Error(e.Message);
            _currentShader = _defaultShader;
        }

        foreach (var (_, value) in _shaderCode)
        {
            value.IsValid = shaderIsValid;
            value.NeedsRebuild = !shaderIsValid;
        }

        ApplyShaderToModel();

        CreateLights(_editorConfiguration.CurrentLightingPreset);
    }

    private void LoadShaderCode()
    {
        // Load shader codes
        _shaderCode = new Dictionary<FileId, ShaderCode>();

        var material = _editorControllerData.MaterialPackage;

        foreach (var fileType in new[] { FileType.VertexShader, FileType.FragmentShader })
        {
            var result = GetShaderCode(material, fileType);
            if (result != null)
                _shaderCode.Add(result.Item1, result.Item2);
        }

        // Determine variables used
        Dictionary<string, CodeVariableBase> allShaderVariables = [];

        foreach (var (key, value) in _shaderCode)
        {
            value.ParseVariables();

            var shaderVariables = value.Variables;

            allShaderVariables = allShaderVariables.Concat(shaderVariables).ToDictionary();
        }


        Logger.Info($"{allShaderVariables.Count} variables detected");


        material.ClearFileReferences();


        // Sync materialMeta variables
        foreach (var (key, variable) in allShaderVariables)
        {
            var result = material.Variables.TryGetValue(key, out var materialVariable);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in materialMeta -> create it");

                var newVariable = CodeVariableFactory.Build(variable.GetType());

                //TODO avoid trick
                if (variable.GetType() == typeof(CodeVariableColor))
                    // Set pink as default color
                    (newVariable as CodeVariableColor).Value = System.Drawing.Color.FromArgb(255, 255, 0, 255);

                material.Variables.Add(key, newVariable);
            }
            else
            {
                if (materialVariable == null)
                    throw new NullReferenceException("material variable is null");

                // already exist => check type change
                if (materialVariable.GetType() != variable.GetType())
                {
                    Logger.Trace($"{key}: type changed");
                    material.Variables[key] = CodeVariableFactory.Build(variable.GetType());
                }

                if (materialVariable.Internal != variable.Internal)
                {
                    Logger.Trace($"{key}: internal flag changed");
                    material.Variables[key].Internal = variable.Internal;
                }
            }
        }

        List<string> toDelete = [];
        foreach (var (key, _) in material.Variables)
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
            material.Variables.Remove(name);
        }

        _editorControllerData.MaterialPackage.UpdateFileReferences();

        Logger.Trace($"{toDelete.Count} variables removed from materialMeta");
    }

    private Tuple<FileId, ShaderCode>? GetShaderCode(MaterialPackage material,
        FileType shaderType)
    {
        var file = material.GetShaderCode(shaderType);
        if (file != null)
        {
            var code = new ShaderCode(System.Text.Encoding.UTF8.GetString(file.Value.Value));
            return new Tuple<FileId, ShaderCode>(file.Value.Key, code);
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
        _camera = new Camera3D(new Vector3(0, 0, -5),
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
            _editorConfiguration.MonitorIndex = Raylib.GetCurrentMonitor();
            var v = Raylib.GetWindowPosition();
            _editorConfiguration.WindowPosition = new Point((int)v.X, (int)v.Y);
            var width = Raylib.GetScreenWidth();
            var height = Raylib.GetScreenHeight();
            _editorConfiguration.WindowSize = new Size(width, height);


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

    private void CreateLights(EditorConfiguration.LightingPreset preset)
    {
        if (_currentShader.HasValue == false)
            throw new NullReferenceException("_currentShader is null");

        LightManager.Clear();
        _editorControllerData.Lights.Clear();

        List<Shader> shaders;

        unsafe
        {
            shaders =
            [
                _currentShader.Value,
                _skyBox.Model.Materials[0].Shader
            ];
        }

        switch (preset)
        {
            case EditorConfiguration.LightingPreset.SingleWhiteLight:
                _editorControllerData.Lights.Add(LightManager.CreateLight(
                    LightType.Point,
                    new Vector3(-2, 1, -2),
                    Vector3.Zero,
                    Color.White,
                    shaders
                ));
                break;

            case EditorConfiguration.LightingPreset.YellowRedGreenBlue:
                _editorControllerData.Lights.Add(LightManager.CreateLight(
                    LightType.Point,
                    new Vector3(-2, 1, -2),
                    Vector3.Zero,
                    Color.Yellow,
                    shaders
                ));
                _editorControllerData.Lights.Add(LightManager.CreateLight(
                    LightType.Point,
                    new Vector3(2, 1, 2),
                    Vector3.Zero,
                    Color.Red,
                    shaders
                ));
                _editorControllerData.Lights.Add(LightManager.CreateLight(
                    LightType.Point,
                    new Vector3(-2, 1, 2),
                    Vector3.Zero,
                    Color.Green,
                    shaders
                ));
                _editorControllerData.Lights.Add(LightManager.CreateLight(
                    LightType.Point,
                    new Vector3(2, 1, -2),
                    Vector3.Zero,
                    Color.Blue,
                    shaders
                ));
                break;


            default:
                throw new ArgumentOutOfRangeException();
        }

        _editorConfiguration.CurrentLightingPreset = preset;
    }

    public void RenderLights()
    {
        foreach (var light in _editorControllerData.Lights)
        {
            Raylib.DrawSphereEx(light.Position, 0.2f, 8, 8, light.Color);
        }
    }

    private void UpdateLights()
    {
        foreach (var light in _editorControllerData.Lights)
        {
            LightManager.UpdateLightValues(light);
        }
    }
}