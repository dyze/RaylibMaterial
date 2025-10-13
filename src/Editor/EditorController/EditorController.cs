using Editor.Configuration;
using Editor.Messaging;
using Editor.Ui;
using Library;
using Library.CodeVariable;
using Library.Lighting;
using Library.Packaging;
using NLog;
using Raylib_cs;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Timer = Editor.Helpers.Timer;

namespace Editor.EditorControllerNS;

internal class EditorController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private bool _initUiOk;

    private Shader? _currentShader;

    /// <summary>
    /// This shader is used if we are not able to load a user one
    /// We proceed like that to prevent crash when trying to use a faulty user shader
    /// </summary>
    private Shader _defaultShader;

    private readonly EditorControllerData _editorControllerData;
    private EditorConfiguration _editorConfiguration = new();

    private readonly string? _startupFilePath;

    // Used to avoid too frequent updates. e.g. when continuously selecting a color in a ImGui Color widget
    private Timer? _timerOnVariablesChanged;


    private readonly EditorUi _editorUi;

    public EditorController(string? filePath)
    {
        _startupFilePath = filePath;

        LoadEditorConfiguration();

        //if (_editorConfiguration.DataFileExplorerConfiguration.DataFolderPath == null)
        //    throw new FileLoadException("DataFolderPath is not in cfg file");

        _editorControllerData = new();

        if (_editorConfiguration.OutputDirectoryPath == "")
            _editorConfiguration.OutputDirectoryPath = Path.GetFullPath($"{EditorControllerData.MaterialsPath}\\");

        _editorControllerData.OutputFilePath = _editorConfiguration.OutputDirectoryPath;
        Directory.CreateDirectory(_editorConfiguration.OutputDirectoryPath);

        DiscoverBuiltInModels();

        if (_editorConfiguration.CurrentModelFilePath == "" ||
            File.Exists(Path.GetFullPath(_editorConfiguration.CurrentModelFilePath)) == false)
        {
            _editorConfiguration.CurrentModelFilePath = _editorControllerData.BuiltInModels.First();
        }

        _editorUi = new EditorUi(_editorControllerData,
            _editorConfiguration);
        _editorUi.SavePressed += EditorUi_SavePressed;
        _editorUi.BuildPressed += EditorUi_BuildPressed;
        _editorUi.SelectModelPressed += EditorUi_SelectModelPressed;
        _editorUi.SkyBoxChanged += SelectSkyBox;
        _editorUi.LightingPresetChangeRequest += CreateLights;
        _editorUi.ResetCameraIsRequest += EditorUi_ResetCamera;
        _editorUi.NewPressed += NewMaterial;
        _editorUi.LoadModelFromFile += LoadModelFromFile;
        _editorUi.LoadMaterial += LoadMaterial;
        _editorUi.SaveAs += SaveAs;
    }

    private void EditorUi_ResetCamera()
    {
        _editorConfiguration.CameraSettings = new CameraSettings();
    }

    private void DiscoverBuiltInModels()
    {
        var path = _editorConfiguration.ResourceModelsPath;
        if (path == null)
            throw new NullReferenceException("path is null");

        _editorControllerData.BuiltInModels = Directory.GetFiles(
                Path.GetFullPath(path), "*.*",
                SearchOption.AllDirectories)
            .Where(file => _editorControllerData.SupportedModelExtensions.Contains(Path.GetExtension(file)))
            .ToList();
    }

    private void EditorUi_SavePressed()
    {
        OnSave();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
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

        var message = Logging.GetLogMessage(new nint(text), new nint(args));

        Logger.Log(level, message);

        ShaderErrorParser.Parse(message, EditorControllerData._shaderCode);
    }

    public void Init()
    {
        Logger.Info("Init...");

        _initUiOk = false;

        unsafe
        {
            Raylib.SetTraceLogCallback(&CustomLog);
        }

        _editorUi.Init();

        _defaultShader = Raylib.LoadShader($"{_editorConfiguration.ResourceShaderFolderPath}\\base.vert",
            $"{_editorConfiguration.ResourceShaderFolderPath}\\base.frag");

        _editorControllerData.ViewTexture = Raylib.LoadRenderTexture(400, 300);

        SelectSkyBox(_editorConfiguration.SkyBox);

        PrepareCamera();

        if (_startupFilePath != null)
            LoadMaterial(_startupFilePath);

        _initUiOk = true;

        Logger.Info("Init OK");
    }

    public void Close()
    {
        Logger.Info("Close...");

        _initUiOk = false;

        Raylib.UnloadShader(_defaultShader);

        _editorControllerData.MaterialPackage.Dispose();

        _editorUi.Close();

        SaveEditorConfiguration();

        Logger.Info("Close OK");
    }

    public void Run()
    {
        Init();

        if (_initUiOk == false)
            throw new ApplicationException("Init has not been called");

        NewMaterial();


        while (true)
        {
            UpdateLights();

            _editorControllerData.MaterialPackage.SetCameraPosition(_editorControllerData.Camera.Position);

            if (_timerOnVariablesChanged != null && _timerOnVariablesChanged.IsElapsed(DateTime.Now))
            {
                _timerOnVariablesChanged = null;
                MaterialPackage_OnVariablesChangedTimerCompletion();
            }

            if (_editorUi.Frame())
                break;
        }

        Close();
    }

    /// <summary>
    /// Clean everything, start with new material
    /// </summary>
    internal void NewMaterial()
    {
        _editorControllerData.MaterialFilePath = null;

        _editorControllerData.MaterialPackage = new();
        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackage_OnFilesChanged;
        _editorControllerData.MaterialPackage.OnShaderChanged += MaterialPackage_OnShaderChanged;
        _editorControllerData.MaterialPackage.OnVariablesChanged += MaterialPackage_OnVariablesChanged;

        _editorControllerData.OutputFilePath =
            $"{_editorConfiguration.OutputDirectoryPath}\\{EditorControllerData.DefaultMaterialName}";

        _editorUi.UpdateWindowCaption();

        AssignDefaultShader();

        EditorControllerData._shaderCode = new();

        LoadModel();
        LoadShaderCode();
        BuildShader();
        AnalyseShaderCode();
        SendVariablesToMaterial();
    }

    private void AssignDefaultShader()
    {
        _currentShader = _defaultShader;
    }


    /// <summary>
    /// Loads the material package set with filePath
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>true if loading OK, false if file can't be read due to file access or unsupported file format, otherwise throws the encountered exception</returns>
    internal bool LoadMaterial(string filePath)
    {
        Logger.Info("LoadMaterial...");
        Logger.Info($"filePath={filePath}");

        try
        {
            _editorControllerData.MaterialPackage = MaterialPackage.Load(filePath);
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException or FileLoadException or DirectoryNotFoundException or IOException
                or NotSupportedException)
            {
                Logger.Error(ex);
                _editorUi.TriggerErrorMessage("Material can't be loaded", ex.Message);
                return false;
            }

            throw;
        }

        _editorControllerData.MaterialPackage.OnFilesChanged += MaterialPackage_OnFilesChanged;
        _editorControllerData.MaterialPackage.OnShaderChanged += MaterialPackage_OnShaderChanged;
        _editorControllerData.MaterialPackage.OnVariablesChanged += MaterialPackage_OnVariablesChanged;

        _editorControllerData.MaterialFilePath = filePath;

        _editorControllerData.OutputFilePath = filePath;
        _editorUi.UpdateWindowCaption();

        _editorConfiguration.AddRecentFile(filePath);

        AssignDefaultShader();

        LoadModel();
        LoadShaderCode();
        BuildShader();
        AnalyseShaderCode();
        SendVariablesToMaterial();

        return true;
    }

    private void MaterialPackage_OnVariablesChanged()
    {
        _timerOnVariablesChanged = null;
        _timerOnVariablesChanged = new Timer(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    private void MaterialPackage_OnVariablesChangedTimerCompletion()
    {
        Logger.Trace("MaterialPackage_OnVariablesChangedTimerCompletion...");

        _editorControllerData.MaterialPackage.UpdateFileReferences();

        SendVariablesToMaterial();
    }


    private void MaterialPackage_OnFilesChanged()
    {
    }

    private void MaterialPackage_OnShaderChanged()
    {
        AssignDefaultShader();
        LoadModel(); // to clean Materials
        LoadShaderCode();
        BuildShader();
        AnalyseShaderCode();
        SendVariablesToMaterial();
    }

    private void EditorUi_BuildPressed()
    {
        AssignDefaultShader();
        LoadModel(); // to clean Materials
        LoadShaderCode();
        BuildShader();
        AnalyseShaderCode();
        SendVariablesToMaterial();
    }


    private void EditorUi_SelectModelPressed(EditorConfiguration.ModelType modelType,
        string modelFilePath)
    {
        Logger.Trace($"{modelType}, {modelFilePath} selected");
        _editorConfiguration.CurrentModelType = modelType;
        _editorConfiguration.CurrentModelFilePath = modelFilePath;
        LoadModel();
    }

    private void LoadModel()
    {
        Dictionary<EditorConfiguration.ModelType, Action> actions = new()
        {
            { EditorConfiguration.ModelType.Cube, () => _editorControllerData.CurrentModel = GenerateCubeModel() },
            { EditorConfiguration.ModelType.Plane, () => _editorControllerData.CurrentModel = GeneratePlaneModel() },
            { EditorConfiguration.ModelType.Sphere, () => _editorControllerData.CurrentModel = GenerateSphereModel() },
            { EditorConfiguration.ModelType.Model, () => LoadModelFromFile(_editorConfiguration.CurrentModelFilePath) },
        };

        actions[_editorConfiguration.CurrentModelType].Invoke();

        Logger.Trace(
            $"MeshCount={_editorControllerData.CurrentModel.MeshCount}, MaterialCount={_editorControllerData.CurrentModel.MaterialCount}");

        ApplyShaderToModel();
    }

    private void LoadModelFromFile(string modelFilePath)
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

        _editorControllerData.CurrentModel = model;

        _editorConfiguration.AddCustomModel(modelFilePath);
    }



    private void OnSave()
    {
        Logger.Info("OnSave...");

        if (_editorControllerData.MaterialFilePath == null)
        {
            _editorUi.OnSaveAsStart();
            return;
        }

        _editorControllerData.MaterialPackage.Save(_editorControllerData.MaterialFilePath);
        _editorConfiguration.AddRecentFile(_editorControllerData.MaterialFilePath);

        Logger.Info("OnSave OK");
    }

    internal void SaveAs(string filePath,
        bool exploreTo = true)
    {
        Logger.Info("SaveAs...");

        _editorControllerData.MaterialFilePath = filePath;

        _editorControllerData.MaterialPackage.Save(_editorControllerData.MaterialFilePath);
        _editorConfiguration.AddRecentFile(_editorControllerData.MaterialFilePath);

        _editorControllerData.OutputFilePath = filePath;
        _editorUi.UpdateWindowCaption();

        if (exploreTo)
        {
            var argument = "/select, \"" + _editorControllerData.MaterialFilePath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        Logger.Info("SaveAs OK");
    }

    private void SelectSkyBox(string? name)
    {
        Logger.Trace($"{name} selected");

        if (name == null || _editorControllerData.SkyBoxes.TryGetValue(name, out var value) == false)
            name = _editorControllerData.SkyBoxes.Keys.First();

        _editorConfiguration.SkyBox = name;
        var background = _editorControllerData.SkyBoxes[name];

        _editorControllerData.SkyBox = new SkyBox(_editorConfiguration);

        var filePath =
            Path.GetFullPath($"{_editorConfiguration.ResourceSkyBoxesFolderPath}/{background.ImageFileName}");
        _editorControllerData.SkyBox.GenerateModel(filePath);
    }

    private void ApplyShaderToModel()
    {
        Logger.Info("ApplyShaderToModel...");

        if (_currentShader.HasValue == false)
            return;

        var shader = _currentShader.Value;

        var materialIndex = Math.Clamp(_editorControllerData.MaterialIndexToEdit, 0,
            _editorControllerData.CurrentModel.MaterialCount - 1);
        if (materialIndex != _editorControllerData.MaterialIndexToEdit)
        {
            Logger.Error($"wrong materialIndex, max is {_editorControllerData.CurrentModel.MaterialCount - 1}");
            _editorControllerData.MaterialIndexToEdit = materialIndex;
        }

        Raylib.SetMaterialShader(ref _editorControllerData.CurrentModel, materialIndex, ref shader);

        Logger.Info("ApplyShaderToModel OK");
    }

    private void SendVariablesToMaterial()
    {
        Logger.Info("SendVariablesToMaterial...");

        var material = Raylib.GetMaterial(ref _editorControllerData.CurrentModel, _editorControllerData.MaterialIndexToEdit);

        _editorControllerData.MaterialPackage.SendVariablesToMaterial(material, true);

        Logger.Info("SendVariablesToMaterial OK");
    }

    private void BuildShader()
    {
        Logger.Info("BuildShader...");

        AssignDefaultShader();

        var materialPackage = _editorControllerData.MaterialPackage;

        materialPackage.UnloadShader();

        var shaderIsValid = false;

        // Clear error messages
        foreach (var (_, value) in EditorControllerData._shaderCode)
        {
            value.Errors.Clear();
        }

        try
        {
            _currentShader = materialPackage.LoadAndBuildShader();
            shaderIsValid = true;
            Logger.Info($"shader id={_currentShader.Value.Id}");
        }
        catch (InvalidDataException e)
        {
            Logger.Error(e.Message);
        }

        foreach (var (_, value) in EditorControllerData._shaderCode)
        {
            value.IsValid = shaderIsValid;
            value.NeedsRebuild = !shaderIsValid;
        }

        ApplyShaderToModel();

        CreateLights(_editorConfiguration.CurrentLightingPreset);

        Logger.Info("BuildShader OK");
    }

    private void AnalyseShaderCode()
    {
        Logger.Trace("AnalyseShaderCode...");

        var material = _editorControllerData.MaterialPackage;


        // Determine variables used in code
        Dictionary<string, CodeVariableBase> allShaderVariables = [];

        foreach (var (_, value) in EditorControllerData._shaderCode)
        {
            value.ParseVariables();

            var shaderVariables = value.Variables;

            allShaderVariables = allShaderVariables.Concat(shaderVariables).ToDictionary();
        }

        Logger.Info($"{allShaderVariables.Count} variables detected");


        // Sync material package variables
        foreach (var (key, variable) in allShaderVariables)
        {
            var result = material.Variables.TryGetValue(key, out var materialVariable);
            if (result == false)
            {
                Logger.Trace($"{key}: doesn't exist in materialVariable -> create it");

                var newVariable = CodeVariableFactory.Build(variable.GetType());

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
            }
        }

        // Remove unused obsolete variables from material package
        List<string> toDelete = [];
        foreach (var (key, _) in material.Variables)
        {
            if (allShaderVariables.ContainsKey(key) == false)
            {
                Logger.Trace($"{key}: doesn't exist in code -> remove from materialMeta");
                toDelete.Add(key);
            }
        }

        foreach (var name in toDelete)
            material.Variables.Remove(name);


        // Finally update file references
        _editorControllerData.MaterialPackage.UpdateFileReferences();

        Logger.Trace($"{toDelete.Count} variables removed from materialMeta");

        Logger.Trace("AnalyseShaderCode OK");
    }

    private void LoadShaderCode()
    {
        // Load shader codes
        EditorControllerData._shaderCode = new Dictionary<FileId, ShaderCode>();

        var material = _editorControllerData.MaterialPackage;

        foreach (var fileType in new[] { FileType.VertexShader, FileType.FragmentShader })
        {
            var result = GetShaderCode(material, fileType);
            if (result != null)
                EditorControllerData._shaderCode.Add(result.Item1, result.Item2);
        }
    }

    private static Tuple<FileId, ShaderCode>? GetShaderCode(MaterialPackage material,
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
        _editorControllerData.Camera = new Camera3D(new Vector3(0, 0, -5),
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

            //TODO move
            var dataFileExplorerConfiguration = _editorConfiguration.DataFileExplorerConfiguration;
            dataFileExplorerConfiguration.SolveFullPaths();
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

        List<Shader> shaders =
        [
            _currentShader.Value
        ];
        if (_editorControllerData.SkyBox.Shader.HasValue)
            shaders.Add(_editorControllerData.SkyBox.Shader.Value);

        var lights = _editorConfiguration.LightingPresets[preset];

        foreach (var light in lights)
        {
            _editorControllerData.Lights.Add(LightManager.CreateLight(
                light.Type,
                light.Position,
                light.Target,
                light.Color,
                light.Intensity,
                shaders
            ));
        }

        _editorConfiguration.CurrentLightingPreset = preset;
    }

    public void UpdateLights()
    {
        foreach (var light in _editorControllerData.Lights)
        {
            LightManager.UpdateLightValues(light);
        }
    }
}