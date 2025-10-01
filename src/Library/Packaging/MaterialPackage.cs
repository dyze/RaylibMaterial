using Library.CodeVariable;
using NLog;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using System.Xml.Linq;
using System;

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

    public MaterialDescription Meta = new();
    private Dictionary<FileId, byte[]> _files = [];
    private Dictionary<FileId, uint> _fileReferences = [];

    [Required] public Dictionary<string, CodeVariableBase> Variables = [];

    /// <summary>
    /// Number of files in package
    /// </summary>
    public int FilesCount => _files.Count;

    /// <summary>
    /// Files embedded in package
    /// </summary>
    public IReadOnlyDictionary<FileId, byte[]> Files => _files;

    public IReadOnlyDictionary<FileId, uint> FileReferences => _fileReferences;


    public bool IsModified { get; set; } = false;

    public void SetModified() => IsModified = true;

    public void ClearModified() => IsModified = false;

    public void TriggerVariablesChanged() => OnVariablesChanged?.Invoke();

    public event Action? OnVariablesChanged;

    /// <summary>
    /// Raylib Shader
    /// </summary>
    public Shader? Shader { get; private set; }


    public MaterialPackage()
    {
    }


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
        { ".jpg", FileType.Image },
        { ".vert", FileType.VertexShader },
        { ".frag", FileType.FragmentShader }
    };


    public static MaterialPackage Load(string packageFilePath)
    {
        Logger Logger = LogManager.GetCurrentClassLogger();

        Logger.Info($"MaterialPackage.Load {packageFilePath}");

        var inputDataAccess = new PackageAccess();
        inputDataAccess.Open(packageFilePath, AccessMode.Read);


        // Read meta file
        Logger.Info($"Reading entry {MetaFileName}...");
        var metaJson = inputDataAccess.ReadTextFile(MetaFileName);

        MaterialMetaFile metaFileObject;
        try
        {
            metaFileObject = MaterialMetaFileStorage.ParseJson(metaJson);
        }
        catch (JsonSerializationException e)
        {
            Logger.Error(e);
            throw new FileLoadException($"{packageFilePath} can't be read. Serialization issue.");
        }

        var materialPackage = new MaterialPackage();

        // Copy fields
        materialPackage.Meta.Author = metaFileObject.Author;
        materialPackage.Meta.Description = metaFileObject.Description;
        materialPackage.Meta.Tags = metaFileObject.Tags;
        materialPackage.Meta.ShaderNames = metaFileObject.ShaderNames;
        materialPackage.Variables = metaFileObject.Variables;

        // Reading files
        foreach (var fileName in inputDataAccess.GetAllFiles())
        {
            if (fileName == MetaFileName)
                continue;

            materialPackage.AddFile(fileName,
                inputDataAccess.ReadBinaryFile(fileName));
        }


        inputDataAccess.Close();

        Logger.Info($"MaterialPackage.Load OK: files read={1 + materialPackage.Files.Count}");

        return materialPackage;
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

        // Copy fields
        var metaFileObject = new MaterialMetaFile
        {
            Author = Meta.Author,
            Description = Meta.Description,
            Tags = Meta.Tags,
            ShaderNames = Meta.ShaderNames,

            Variables = Variables
        };

        // Add meta file
        var metaJson = MaterialMetaFileStorage.ToJson(metaFileObject);
        Logger.Info($"Adding entry {MetaFileName}...");

        outputDataAccess.AddTextFile(metaJson, MetaFileName);


        // Add files
        foreach (var file in _files)
        {
            Logger.Info($"Adding entry {file}...");

            outputDataAccess.AddBinaryFile(file.Value, file.Key.FileName);
        }


        outputDataAccess.Close();

        ClearModified();

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

    public byte[] GetFile(FileType fileType, string fileName) =>
        Files.GetValueOrDefault(new FileId(fileType, fileName));

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
        uint count = 1)
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

        if (Shader.HasValue)
            return Shader.Value;

        Shader = Raylib.LoadShaderFromMemory(
            vertexShader != null ? System.Text.Encoding.UTF8.GetString(vertexShader.Value.Value) : null,
            fragmentShader != null ? System.Text.Encoding.UTF8.GetString(fragmentShader.Value.Value) : null);

        if (Shader == null)
            throw new InvalidDataException("Shader can't be instantiated");

        var valid = Raylib.IsShaderValid(Shader.Value);

        if (valid == false)
        {
            Raylib.UnloadShader(Shader.Value);
            Shader = null;
            throw new InvalidDataException("Shader is not valid");
        }

        unsafe
        {
            Shader.Value.Locs[(int)ShaderLocationIndex.VectorView] =
                Raylib.GetShaderLocation(Shader.Value, "viewPos");
            //Shader.Value.Locs[(int)ShaderLocationIndex.MapAlbedo] =
            //    Raylib.GetShaderLocation(Shader.Value, "albedoMap");
        }

        return Shader.Value;
    }

    public void UnloadShader()
    {
        if (Shader.HasValue)
            Raylib.UnloadShader(Shader.Value);
        Shader = null;
    }

    public void Dispose()
    {
        UnloadShader();
    }

    public void SendVariablesToModel(Model model, bool force)
    {
        Logger.Info("SendVariablesToModel...");

        if (Shader.HasValue == false)
            return;

        foreach (var (name, variable) in Variables)
        {
            if (force == false)
                if (variable.SendToShader == false)
                    continue;
                else
                    Logger.Trace($"{name} changed");

            variable.SendToShader = false;

            var location = Raylib.GetShaderLocation(Shader.Value, name);
            if (location < 0)
            {
                Logger.Error($"location for {name} not found in shader. maybe because unused in code");
                continue;
            }

            if (variable.GetType() == typeof(CodeVariableInt))
            {
                var currentValue = (variable as CodeVariableInt).Value;
                Raylib.SetShaderValue(Shader.Value, location, currentValue, ShaderUniformDataType.Int);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableVector2))
            {
                var currentValue = (variable as CodeVariableVector2).Value;
                Raylib.SetShaderValue(Shader.Value, location, currentValue, ShaderUniformDataType.Vec2);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableVector3))
            {
                var currentValue = (variable as CodeVariableVector3).Value;
                Raylib.SetShaderValue(Shader.Value, location, currentValue, ShaderUniformDataType.Vec3);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableVector4))
            {
                var currentValue = (variable as CodeVariableVector4).Value;
                Raylib.SetShaderValue(Shader.Value, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableColor))
            {
                var currentValue = TypeConverters.ColorToVector4((variable as CodeVariableColor).Value);
                Raylib.SetShaderValue(Shader.Value, location, currentValue, ShaderUniformDataType.Vec4);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableFloat))
            {
                var currentValue = (variable as CodeVariableFloat).Value;
                Raylib.SetShaderValue(Shader.Value, location, currentValue, ShaderUniformDataType.Float);
                Logger.Trace($"{name}={currentValue}");
            }
            else if (variable.GetType() == typeof(CodeVariableTexture))
            {
                var materialMapIndex = (variable as CodeVariableTexture).MaterialMapIndex;

                if(materialMapIndex==null)
                    Logger.Error($"{name}: materialMapIndex not set ");
                else
                    SetUniformTexture(name, 
                        (variable as CodeVariableTexture).Value, 
                        model,
                        materialMapIndex.Value);
            }
            else
                Logger.Error($"{variable.GetType()} not supported");
        }
    }

    private void SetUniformTexture(string variableName,
        string fileName,
        Model model,
        MaterialMapIndex materialMapIndex)
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

        var image = Raylib.LoadImageFromMemory(extension, file); // ignore period
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


        Raylib.SetMaterialTexture(ref model, 0, materialMapIndex, ref texture);
        Logger.Trace($"{variableName}={fileName}");

        //unsafe
        //{
        //    var currentValue = model.Materials[0].Maps[(int)materialMapIndex].Value;
        //    Logger.Trace($"{variableName}: currentValue={currentValue}");
        //    model.Materials[0].Maps[(int)materialMapIndex].Value = 1f;

        //    model.Materials[0].Maps[(int)materialMapIndex].Color = new Color(1, 1, 1, 1);
        //}

        //if (Type.TryGetValue(variableName, out var index))
        //{
        //    Raylib.SetMaterialTexture(ref model, 0, index, ref texture);
        //    Logger.Error($"{variableName}={fileName}");
        //}
        //else
        //{
        //    Logger.Warn($"MaterialMapIndex for {variableName} can't be found");

        //    var location = Raylib.GetShaderLocation(Shader.Value, variableName);
        //    if (location < 0)
        //        Logger.Error($"location for {variableName} not found in shader. maybe because unused in code");
        //    else
        //    {
        //        Raylib.SetShaderValue(Shader.Value, location, texture, ShaderUniformDataType.Sampler2D);
        //        Logger.Trace($"{variableName}={fileName}");
        //    }
        //}
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
                var count = Variables.Count(v =>
                    v.Value.GetType() == typeof(CodeVariableTexture)
                    && (v.Value as CodeVariableTexture)?.Value == key.FileName);
                IncFileReferences(key, (uint)count);
            }
        }
    }

    public void ActivateShader(FileId fileKey)
    {
        SetShaderName(fileKey.FileType, fileKey.FileName);
    }

    public void SetCameraPosition(Vector3 cameraPosition)
    {
        if (Shader == null)
            return;

        if (Variables.TryGetValue("viewPos", out var variable))
        {
            var v = variable as CodeVariableVector3;
            v.Value = cameraPosition;
        }

        unsafe
        {
            Raylib.SetShaderValue(
                Shader.Value,
                Shader.Value.Locs[(int)ShaderLocationIndex.VectorView],
                cameraPosition,
                ShaderUniformDataType.Vec3
            );
        }
    }
}