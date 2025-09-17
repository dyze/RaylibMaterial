using Microsoft.VisualBasic.FileIO;
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

    public const string MetaFileName = "material.meta";

    public MaterialMeta Meta = new();
    private Dictionary<FileId, byte[]> _files = [];

    public int FilesCount => _files.Count;

    public IReadOnlyDictionary<FileId, byte[]> Files => _files;

    public MaterialPackage()
    {}


    public void Clear()
    {
        Meta = new();
        _files = [];
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
            if(fileName == MetaFileName)
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

        Directory.CreateDirectory(Path.GetDirectoryName(outputPackageFilePath));

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

        Logger.Info($"MaterialPackage.BuildPackage OK: files added={1 + _files.Count}");
    }

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

    public KeyValuePair<FileId, byte[]>? GetFileOfType(FileType fileType)
    {
        return Files.Where(f => f.Key.FileType == fileType)
            .Select(e => (KeyValuePair<FileId, byte[]>?)e)
            .FirstOrDefault();
    }
}