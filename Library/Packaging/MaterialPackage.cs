using Library.CodeVariable;
using Library.Helpers;
using NLog;
using Raylib_cs;

namespace Library.Packaging;

public enum FileType
{
    Unknown = 0,
    VertexShader,
    FragmentShader,
    Image,
}

public class MaterialPackage : IDisposable
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Triggered when a file is added or removed
    /// </summary>
    public event Action? OnFilesChanged;

    /// <summary>
    /// Triggered when Shader is changed
    /// </summary>
    public event Action? OnShaderChanged;

    public const string MetaFileName = "material.meta";

    public MaterialMeta Meta = new();
    private Dictionary<FileId, byte[]> _files = [];
    private Dictionary<FileId, uint> _fileReferences = [];

    /// <summary>
    /// Number of files in package
    /// </summary>
    public int FilesCount => _files.Count;

    /// <summary>
    /// Files embedded in package
    /// </summary>
    public IReadOnlyDictionary<FileId, byte[]> Files => _files;

    public IReadOnlyDictionary<FileId, uint> FileReferences => _fileReferences;


    public MaterialPackage()
    { }


    public void Clear()
    {
        Meta = new();
        _files = [];
        _fileReferences = [];
    }

    public void ClearFileReferences()
    {
        _fileReferences = [];
    }

    public static Dictionary<string, FileType> ExtensionToFileType = new()
    {
        { ".png", FileType.Image },
        { ".vert", FileType.VertexShader },
        { ".frag", FileType.FragmentShader }
    };

    /// <summary>
    /// Raylib Shader
    /// </summary>
    private Shader? _shader;

    public void Load(string packageFilePath)
    {
        Logger.Info($"MaterialPackage.Load {packageFilePath}");

        Clear();

        var inputDataAccess = new PackageAccess();
        inputDataAccess.Open(packageFilePath, AccessMode.Read);


        // Read meta file
        Logger.Info($"Reading entry {MetaFileName}...");
        var metaJson = inputDataAccess.ReadTextFile(MetaFileName);

        Meta = MaterialMetaStorage.ParseJson(metaJson);

        // Reading files
        foreach (var fileName in inputDataAccess.GetAllFiles())
        {
            if (fileName == MetaFileName)
                continue;

            AddFile(fileName,
                inputDataAccess.ReadBinaryFile(fileName));
        }


        inputDataAccess.Close();

        Meta.ClearModified();

        Logger.Info($"MaterialPackage.Load OK: files read={1 + _files.Count}");
    }


    public void Save(string outputPackageFilePath)
    {
        Logger.Info($"MaterialPackage.Save {outputPackageFilePath}");

        var directoryPath = Path.GetDirectoryName(outputPackageFilePath);
        if (directoryPath == null)
            throw new NullReferenceException($"directory path can't be extracted from {outputPackageFilePath}");

        Directory.CreateDirectory(directoryPath);

        if (File.Exists(outputPackageFilePath))
        {
            var backupFilePath = $"{outputPackageFilePath}.bck";
            Logger.Info($"Creating backup {backupFilePath}...");
            File.Copy(outputPackageFilePath, backupFilePath, true);
        }

        var outputDataAccess = new PackageAccess();
        outputDataAccess.Open(outputPackageFilePath, AccessMode.Create);

        // Add meta file
        var metaJson = MaterialMetaStorage.ToJson(Meta);
        Logger.Info($"Adding entry {MetaFileName}...");

        outputDataAccess.AddTextFile(metaJson, MetaFileName);


        // Add files
        foreach (var file in _files)
        {
            Logger.Info($"Adding entry {file}...");

            outputDataAccess.AddBinaryFile(file.Value, file.Key.FileName);
        }


        outputDataAccess.Close();

        Meta.ClearModified();

        Logger.Info($"MaterialPackage.Save OK: files added={1 + _files.Count}");
    }


    public void AddFile(string fileName,
        byte[] fileContent)
    {
        var extension = Path.GetExtension(fileName);
        FileType? fileType = ExtensionToFileType.GetValueOrDefault(extension);
        if (fileType == null)
            fileType = FileType.Unknown;

        var fileId = new FileId(fileType.Value, fileName);

        if (_files.TryAdd(fileId,
                fileContent) == false)
        {
            Logger.Error($"{fileName} is already in the list");
            return;
        }

        CreateFileReferences(fileId);

        OnFilesChanged?.Invoke();
    }

    public byte[] GetFile(FileType fileType, string fileName) => Files.GetValueOrDefault(new FileId(fileType, fileName));

    public KeyValuePair<FileId, byte[]>? GetFileMatchingType(FileType fileType)
    {
        return Files.Where(f => f.Key.FileType == fileType)
            .Select(e => (KeyValuePair<FileId, byte[]>?)e)
            .FirstOrDefault();
    }

    public void SetShaderName(FileType shaderType, string shaderName)
    {
        Meta.SetShaderName(shaderType, shaderName);

        OnShaderChanged?.Invoke();
    }

    public string? GetShaderName(FileType shaderType) => Meta.GetShaderName(shaderType);


    public KeyValuePair<FileId, byte[]>? GetShaderCode(FileType shaderType)
    {
        var shaderName = Meta.GetShaderName(shaderType);
        if (shaderName == null)
            return null;

        return Files.Where(f => f.Key.FileName == shaderName)
            .Select(e => (KeyValuePair<FileId, byte[]>?)e)
            .FirstOrDefault();
    }

    public void CreateFileReferences(FileId key)
    {
        if (_fileReferences.ContainsKey(key) == false)
            _fileReferences.Add(key, 0);
    }

    public void IncFileReferences(FileId key,
        uint count=1)
    {
        _fileReferences[key] += count;
    }

    public void DeleteFile(FileId fileId)
    {
        var fileReferences = FileReferences[fileId];
        if (fileReferences > 0)
            throw new ApplicationException($"{fileId} can't be removed because it is still in use");

        _files.Remove(fileId);
        _fileReferences.Remove(fileId);

        Logger.Info($"{fileId} has been removed from package.");
    }

    public void UpdateFile(FileId fileId, byte[] data)
    {
        _files[fileId] = data;
    }

    /// <summary>
    /// Loads the main shader defined inside the package
    /// </summary>
    /// <returns>A raylib Shader object</returns>
    /// <exception cref="InvalidDataException">If the Shader is not valid</exception>
    public Shader LoadShader()
    {
        var vertexShader = GetShaderCode(FileType.VertexShader);
        var fragmentShader = GetShaderCode(FileType.FragmentShader);

        if (_shader.HasValue)
            return _shader.Value;

        _shader = Raylib.LoadShaderFromMemory(
            vertexShader != null ? System.Text.Encoding.UTF8.GetString(vertexShader.Value.Value) : null,
            fragmentShader != null ? System.Text.Encoding.UTF8.GetString(fragmentShader.Value.Value) : null);

        if (_shader == null)
            throw new InvalidDataException("Shader can't be instantiated");

        var valid = Raylib.IsShaderValid(_shader.Value);

        if (valid == false)
        {
            Raylib.UnloadShader(_shader.Value);
            _shader = null;
            throw new InvalidDataException("Shader is not valid");
        }

        return _shader.Value;
    }

    public void UnloadShader()
    {
        if (_shader.HasValue)
            Raylib.UnloadShader(_shader.Value);
        _shader = null;
    }

    public void Dispose()
    {
        UnloadShader();
    }

    public void ApplyVariablesToModel(Model model)
    {
        Logger.Info("ApplyVariablesToModel...");

        if (_shader.HasValue == false)
            return;

        foreach (var (name, variable) in Meta.Variables)
        {
            var location = Raylib.GetShaderLocation(_shader.Value, name);
            if (location < 0)
            {
                Logger.Error($"location for {name} not found in shader. maybe because unused in code");
                continue;
            }

            if (variable.GetType() == typeof(CodeVariableVector4))
            {
                var currentValue = (variable as CodeVariableVector4).Value;
                Raylib.SetShaderValue(_shader.Value, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableColor))
            {
                var currentValue = TypeConvertors.ColorToVector4((variable as CodeVariableColor).Value);
                Raylib.SetShaderValue(_shader.Value, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableFloat))
            {
                var currentValue = (variable as CodeVariableFloat).Value;
                Raylib.SetShaderValue(_shader.Value, location, currentValue, ShaderUniformDataType.Float);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableTexture))
            {
                SetUniformTexture(name, (variable as CodeVariableTexture).Value, model);
            }
        }
    }

    private void SetUniformTexture(string variableName,
        string fileName, Model model)
    {
        Logger.Info("SetUniformTexture...");

        if (fileName == "")
        {
            Logger.Trace("No filename set");
            return;
        }

        var extension = Path.GetExtension(fileName);
        if (extension == null)
            throw new NullReferenceException($"No file extension found in {fileName}");

        var file = GetFile(FileType.Image, fileName);
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
            Raylib.SetMaterialTexture(ref model, 0, index, ref texture);
        }
        else
            Logger.Error($"texture index for {variableName} can't be found");

        Logger.Trace($"{variableName}={fileName}");
    }

    public void UpdateFileReferences()
    {
        ClearFileReferences();

        foreach (var (key, _) in Files)
        {
            CreateFileReferences(key);

            if (key.FileType == FileType.VertexShader ||
                key.FileType == FileType.FragmentShader)
            {
                if (GetShaderName(key.FileType) == key.FileName)
                    IncFileReferences(key);
            }
            else if (key.FileType == FileType.Image)
            {
                var count = Meta.Variables.Count(v =>
                    v.Value.GetType() == typeof(CodeVariableTexture)
                    && (v.Value as CodeVariableTexture)?.Value == key.FileName);
                IncFileReferences(key, (uint)count);
            }
        }
    }
}