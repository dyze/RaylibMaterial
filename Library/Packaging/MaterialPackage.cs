using NLog;

namespace Library.Packaging;

public enum FileType
{
    Unknown = 0,
    VertexShader = 0,
    FragmentShader,
    Image,
}

public struct FileId
{
    public FileType FileType;
    public string FileName;

    public FileId(FileType fileType, string fileName)
    {
        FileType = fileType;
        FileName = fileName;
    }

    public override string ToString() => $"{FileType}/{FileName}";
}

public class MaterialPackage
{
    public const string MetaFileName = "material.meta";
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public MaterialMeta Meta = new();
    public Dictionary<FileId, byte[]> Files = [];

    public void Clear()
    {
        Meta = new();
        Files = [];
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

        Logger.Info($"MaterialPackage.Load OK: files read={1 + Files.Count}");
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
        foreach (var file in Files)
        {
            Logger.Info($"Adding entry {file}...");

            outputDataAccess.AddBinaryFile(file.Value, file.Key.FileName);
        }


        outputDataAccess.Close();

        Logger.Info($"MaterialPackage.BuildPackage OK: files added={1 + Files.Count}");
    }

    public void AddFile(string fileName,
        byte[] fileContent)
    {
        var extension = Path.GetExtension(fileName);
        FileType? fileType = ExtensionToFileType.GetValueOrDefault(extension);
        if (fileType == null)
            fileType = FileType.Unknown;

        if (Files.TryAdd(new FileId(fileType.Value, fileName),
                fileContent) == false)
            Logger.Error($"{fileName} is already in the list");
    }
}