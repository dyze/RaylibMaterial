using Newtonsoft.Json;
using NLog;

namespace Library.Packaging;

public enum FileType
{
    Unknown = 0,
    VertexShader,
    FragmentShader,
    Image,
}

public class MaterialPackage
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public event Action? OnFilesChanged;
    public event Action? OnShaderChanged;

    public const string MetaFileName = "material.meta";

    public MaterialMeta Meta = new();
    private Dictionary<FileId, byte[]> _files = [];
    private Dictionary<FileId, uint> _fileReferences = [];

    public int FilesCount => _files.Count;

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

        Logger.Info($"MaterialPackage.Load OK: files read={1 + _files.Count}");
    }


    public void Save(string outputPackageFilePath)
    {
        Logger.Info($"MaterialPackage.Save {outputPackageFilePath}");

        var directoryPath = Path.GetDirectoryName(outputPackageFilePath);
        if (directoryPath == null)
            throw new NullReferenceException($"directory path can't be extracted from {outputPackageFilePath}");

        Directory.CreateDirectory(directoryPath);

        var outputDataAccess = new PackageAccess();

        outputDataAccess.Open(outputPackageFilePath, AccessMode.Create);

        if (outputDataAccess.Exists())
        {
            var backupFilePath = $"{outputPackageFilePath}.bck";
            Logger.Info($"Creating backup {backupFilePath}...");
            File.Copy(outputPackageFilePath, backupFilePath, true);
        }

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

        Logger.Info($"MaterialPackage.Save OK: files added={1 + _files.Count}");
    }

    //public void Open(string filePath)
    //{
    //    Logger.Info($"MaterialPackage.Open {filePath}");

    //    var inputDataAccess = new PackageAccess();

    //    inputDataAccess.Open(filePath, AccessMode.Read);

    //    // read meta file
    //    Logger.Info($"Reading entry {MetaFileName}...");

    //    var metaJson = inputDataAccess.ReadTextFile(MetaFileName);

    //    Meta = MaterialMetaStorage.ParseJson(metaJson);

    //    foreach (var fileName in inputDataAccess.GetAllFiles())
    //    {
    //        if (fileName == MetaFileName)
    //            continue;

    //        Logger.Info($"Reading entry {fileName}...");

    //        var fileContent = inputDataAccess.ReadBinaryFile(fileName);
    //        AddFile(fileName, fileContent);
    //    }

    //    inputDataAccess.Close();
    //    inputDataAccess = null;
    //}

    public void AddFile(string fileName,
        byte[] fileContent)
    {
        var extension = Path.GetExtension(fileName);
        FileType? fileType = ExtensionToFileType.GetValueOrDefault(extension);
        if (fileType == null)
            fileType = FileType.Unknown;

        if (_files.TryAdd(new FileId(fileType.Value, fileName),
                fileContent) == false)
        {
            Logger.Error($"{fileName} is already in the list");
            return;
        }

        OnFilesChanged?.Invoke();
    }

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


}