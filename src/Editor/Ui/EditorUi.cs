using Editor.Configuration;
using Editor.EditorControllerNS;
using Editor.Helpers;
using Editor.Ui.Windows;
using ImGuiNET;
using Library.Dialogs;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;

namespace Editor.Ui;


internal class EditorUi : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly EditorControllerData _editorControllerData;

    private string WindowCaption => $"Raylib Material Editor - {_editorControllerData.OutputFilePath}";

    private readonly OutputWindow _outputWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly MessageWindow _messageWindow;
    private readonly DataFileExplorer _dataFileExplorer;
    private readonly MaterialWindow _materialWindow;
    private readonly CodeWindow _shaderCodeWindow;
    private readonly Dictionary<string, ImageWindow> _imageWindows = [];

    private FileDialogInfo? _fileDialogInfo;
    private MessageDialog.Configuration? _messageDialogConfiguration;
    private readonly EditorConfiguration _editorConfiguration;

    // We can remove windows while rendering, we postpone
    private readonly List<string> _imageWindowsToRemove = [];

    private bool _windowSizeChanged; // set to true when switching to fullscreen
    private Vector2 _previousMousePosition;

    private bool _processingRequestToClose;
    private bool _requestToCloseAccepted;
    private bool _requestToClose;

    public Action? NewPressed;
    public Action? SavePressed;
    public Action? BuildPressed;
    public Action<EditorConfiguration.ModelType, string>? SelectModelPressed;
    public event Action<string>? SkyBoxChanged;

    public event Action<EditorConfiguration.LightingPreset>? LightingPresetChangeRequest;
    public event Action? ResetCameraIsRequest;
    public event Action? SaveEditorConfiguration;
    public event Action<string>? LoadModelFromFile;
    public event Func<string, bool>? LoadMaterial;
    public event Action<string, bool>? SaveAs;

    public EditorUi(EditorControllerData editorControllerData,
        EditorConfiguration editorConfiguration)
    {
        _editorControllerData = editorControllerData;
        _editorConfiguration = editorConfiguration;

        _messageWindow = new(_editorControllerData);

        _dataFileExplorer = new(_editorConfiguration, _editorControllerData,
            _editorControllerData.DataFileExplorerData);

        _dataFileExplorer.ImageOpenRequest += DateFileExplorer_ImageOpenRequest;

        _shaderCodeWindow = new(_editorConfiguration,
            _editorControllerData);

        _materialWindow = new(_editorControllerData);
        _materialWindow.OnSave += _materialWindow_OnSave;

        _materialWindow._variablesControls.ImageOpenRequest += ImageOpenRequest;

        _shaderCodeWindow.BuildPressed += CodeWindow_OnBuildPressed;

        _outputWindow = new(_editorConfiguration,
            _editorControllerData);

        _outputWindow.ModelTypeChangeRequest += SelectModel;
        _outputWindow.SkyBoxChanged += SelectSkyBox;
        _outputWindow.LightingPresetChangeRequest += (preset) => LightingPresetChangeRequest?.Invoke(preset);
        _outputWindow.ResetCameraIsRequest += () => ResetCameraIsRequest?.Invoke();

        _settingsWindow = new(_editorConfiguration);
        _settingsWindow.SavePressed += () => SaveEditorConfiguration?.Invoke();
    }


    public void Dispose()
    {
        Close();
    }

    public void Init()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint |
                              ConfigFlags.ResizableWindow); // Enable Multi Sampling Anti Aliasing 4x (if available)

        Raylib.InitWindow(_editorConfiguration.WindowSize.Width, _editorConfiguration.WindowSize.Height, WindowCaption);

        Raylib.SetWindowMonitor(_editorConfiguration.MonitorIndex);
        Raylib.SetWindowPosition(_editorConfiguration.WindowPosition.X, _editorConfiguration.WindowPosition.Y);

        Raylib.SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();

        _dataFileExplorer.PrepareUi();

        _previousMousePosition = Raylib.GetMousePosition();

        Raylib.SetTargetFPS(60);

        LoadUiResources();
        DiscoverSkyBoxes();
    }

    public void Close()
    {
        rlImGui.Shutdown();
    }

    private void LoadUiResources()
    {
        foreach (var (_, tool) in _editorControllerData.Tools)
        {
            var image = Raylib.LoadImage($"{_editorConfiguration.ResourceToolBoarFolderPath}/{tool.ImageFileName}");
            tool.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
        }
    }

    private void DiscoverSkyBoxes()
    {
        var files = Directory.GetFiles(Path.GetFullPath(_editorConfiguration.ResourceSkyBoxesFolderPath), "*.*",
                SearchOption.AllDirectories)
            .Where(file => _editorControllerData.SupportedImagesExtensions.Contains(Path.GetExtension(file)))
            .ToList();

        _editorControllerData.SkyBoxes = new();

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var config = new SkyBoxConfig(fileName, Path.GetFileName(filePath));
            var image = Raylib.LoadImage(filePath);
            config.Texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);

            _editorControllerData.SkyBoxes.Add(fileName, config);
        }
    }

    private void HandleImageWindows()
    {
        while (_imageWindowsToRemove.Count > 0)
        {
            var filePath = _imageWindowsToRemove.First();
            _imageWindowsToRemove.Remove(filePath);
            _imageWindows.Remove(filePath);
            break;
        }

        foreach (var (_, imageWindow) in _imageWindows)
        {
            imageWindow.Render();
        }
    }

    private void ImageOpenRequest(string imageName, byte[] imageData)
    {
        PopImageWindow(imageName, imageData);
    }

    private void DateFileExplorer_ImageOpenRequest(string imageFilePath)
    {
        var imageData = File.ReadAllBytes(imageFilePath);

        PopImageWindow(imageFilePath, imageData);
    }

    private void PopImageWindow(string imageFilePath, byte[] imageData)
    {
        var imageWindow = new ImageWindow(imageFilePath, imageData);

        imageWindow.CloseRequest += CloseRequest;

        void CloseRequest(ImageWindow imageWindow)
        {
            var found = _imageWindows.ContainsKey(imageFilePath);
            if (found == false)
                throw new NullReferenceException($"Window {imageFilePath} not found");

            _imageWindowsToRemove.Add(imageFilePath);
        }

        _imageWindows.Add(imageFilePath, imageWindow);
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

                var extension = Path.GetExtension(modelPath);
                if (_editorControllerData.SupportedModelExtensions.Contains(extension))
                {
                    _editorConfiguration.CurrentModelFilePath = modelPath;
                    _editorConfiguration.CurrentModelType = EditorConfiguration.ModelType.Model;
                    LoadModelFromFile(modelPath);
                }
                else
                    Logger.Error(
                        $"extension {extension} is not supported, only {string.Join(",", _editorControllerData.SupportedModelExtensions)} are");
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
                    SavePressed?.Invoke();

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
                        _ => NewPressed?.Invoke(),
                        System.Drawing.Color.Red),
                    new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.No, "No, I changed my mind"
                    )
                ]);
        }
        else
            NewPressed?.Invoke();
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
                                LoadModelFromFile?.Invoke(filePath);
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
                LoadMaterial?.Invoke(filePath);
        }
    }
    private void LoadMaterialAskForFile()
    {
        Logger.Info("LoadMaterialAskForFile...");

        var directoryName = Path.GetDirectoryName(_editorControllerData.OutputFilePath);
        if (directoryName == null)
            throw new NullReferenceException($"directory name can't be extracted from {_editorControllerData.OutputFilePath}");

        _fileDialogInfo = new()
        {
            Title = "Please select a material",
            Type = ImGuiFileDialogType.OpenFile,
            DirectoryPath = new DirectoryInfo(directoryName),
            FileName = "",
            Extensions =
            [
                new Tuple<string, string>("*" + EditorControllerData.MaterialFileExtension, "Materials"),
                new Tuple<string, string>("*" + EditorControllerData.MaterialBackupFileExtension, "Material backups")
            ]
        };

        Logger.Info("LoadMaterialAskForFile OK");
    }

    private void RenderModels()
    {
        Raylib.BeginTextureMode(_editorControllerData.ViewTexture);

        Raylib.BeginMode3D(_editorControllerData.Camera);
        Raylib.ClearBackground(Color.Black);

        Rlgl.DisableBackfaceCulling();
        Rlgl.DisableDepthMask();
        Raylib.DrawModel(_editorControllerData.SkyBox.Model, Vector3.Zero, 1f, Color.White);
        Rlgl.EnableBackfaceCulling();
        Rlgl.EnableDepthMask();

        Raylib.DrawModel(_editorControllerData.CurrentModel, Vector3.Zero, _editorConfiguration.ModelScale, Color.White);

        if (_editorConfiguration.IsInDebugMode)
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

        _editorControllerData.Camera.Target = cameraSettings.Target;
        _editorControllerData.Camera.Position = v;

        _previousMousePosition = currentPosition;
    }
    public void OnSaveAsStart()
    {
        Logger.Info("OnSaveAsStart...");

        var directoryName = Path.GetDirectoryName(_editorControllerData.OutputFilePath);
        if (directoryName == null)
            throw new NullReferenceException($"directory name can't be extracted from {_editorControllerData.OutputFilePath}");

        _fileDialogInfo = new()
        {
            Title = "Please select a material",
            Type = ImGuiFileDialogType.SaveFile,
            DirectoryPath = new DirectoryInfo(directoryName),
            FileName = Path.GetFileName(_editorControllerData.OutputFilePath),
            Extensions =
            [
                new Tuple<string, string>("*" + EditorControllerData.MaterialFileExtension, "Materials")
            ]
        };

        Logger.Info("OnSaveAsStart OK");
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
                                _ => SaveAs?.Invoke(filePath, true),
                                System.Drawing.Color.Red),
                            new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.No, "No, I changed my mind"
                            )
                        ]);
                }
                else
                    SaveAs?.Invoke(_fileDialogInfo.ResultPath, true);
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

    public void RenderLights()
    {
        foreach (var light in _editorControllerData.Lights)
        {
            Raylib.DrawSphereEx(light.Position, 0.2f, 8, 8, light.Color);
        }
    }

    public void UpdateWindowCaption()
    {
        Raylib.SetWindowTitle(WindowCaption);
    }

    public void TriggerErrorMessage(string caption, string message)
    {
        _messageDialogConfiguration = new(caption,
            message,
            [
                new MessageDialog.ButtonConfiguration(MessageDialog.ButtonId.Ok, "Continue", null,
                    System.Drawing.Color.OrangeRed)
            ]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>true if exit requested</returns>
    public bool Frame()
    {
        if (_requestToCloseAccepted)
            return true;
        if (Raylib.WindowShouldClose() || _requestToClose)
        {
            _requestToClose = false;
            if (RequestCloseAccepted())
                return true;
        }

        HandleWindowResize();
        HandleMouseMovement();
        HandleFileDrop();


        Raylib.BeginDrawing();
        rlImGui.Begin();

        Raylib.ClearBackground(Color.Black);

        RenderMenu();
        RenderModels();

        RenderFileDialog();
        RenderMessageDialog();
        _settingsWindow.Render();

        HandleImageWindows();

        var codeIsModified = _shaderCodeWindow.Render(EditorControllerData._shaderCode);
        if (codeIsModified)
        {
            foreach (var (key, value) in EditorControllerData._shaderCode)
            {
                var array = System.Text.Encoding.UTF8.GetBytes(value.Code);
                _editorControllerData.MaterialPackage.UpdateFile(key, array);
            }

            _editorControllerData.MaterialPackage.SetModified();
        }

        _outputWindow.RenderOutputWindow();
        _materialWindow.Render();

        _messageWindow.Render(EditorControllerData.MessageQueue,
            ref _editorConfiguration.WorkspaceConfiguration.MessageWindowIsVisible);

        _dataFileExplorer.Render();

        rlImGui.End();
        Raylib.EndDrawing();

        if (_editorControllerData.WorkspaceLayoutResetRequested)
        {
            Logger.Trace("WorkspaceLayoutReset done");
            _editorControllerData.WorkspaceLayoutResetRequested = false;
        }

        return false;
    }

    private void _materialWindow_OnSave()
    {
        SavePressed?.Invoke();
    }

    private void CodeWindow_OnBuildPressed()
    {
       BuildPressed?.Invoke();
    }

    private void SelectModel(EditorConfiguration.ModelType modelType,
        string modelFilePath)
    {
        SelectModelPressed?.Invoke(modelType, modelFilePath);
    }

    private void SelectSkyBox(string name)
    {
        SkyBoxChanged?.Invoke(name);
    }

}