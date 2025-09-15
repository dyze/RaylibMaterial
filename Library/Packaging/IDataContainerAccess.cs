namespace Library.Packaging;

public enum AccessMode
{
    Read = 0,
    Create = 1,
    ReadWrite = 2,
}

public class FolderContent
{
    public readonly Dictionary<string, FolderContent> Folders = new();
    public readonly List<string> Files = new();
    public string FullPath { get; private set; }
    public string RelativePath { get; private set; }

    public FolderContent(string fullPath,
        string relativePath)
    {
        FullPath = fullPath;
        RelativePath = relativePath;
    }

    public void Clear()
    {
        Folders.Clear();
        FullPath = "";
        RelativePath = "";
    }
}

/// <summary>
/// Interface to access a data container
/// </summary>
public interface IDataContainerAccess
{
    /// <summary>
    /// Open the container in a specific accessMode
    /// </summary>
    /// <param name="containerPath">Exact meaning depends on implementation. It can be a folder path, a file path or something else</param>
    /// <param name="accessMode"></param>
    void Open(string containerPath,
        AccessMode accessMode);

    /// <summary>
    /// Close the opened container
    /// </summary>
    void Close();

    /// <summary>
    /// Checks container existence
    /// </summary>
    /// <returns>true if the container exists</returns>
    bool Exists();

    /// <summary>
    /// Add an entry to the container using an existing file
    /// </summary>
    /// <param name="sourceFilePath">Path of existing file</param>
    /// <param name="destinationFilePath">location of the new file inside the container</param>
    void AddFileUsingOtherFile(string sourceFilePath,
        string destinationFilePath);

    /// <summary>
    /// Add an entry to the container using data in memory
    /// </summary>
    /// <param name="sourceData"></param>
    /// <param name="filePath">location of the path inside the container</param>
    void AddBinaryFile(byte[] sourceData,
        string filePath);

    /// <summary>
    /// Add an entry to the container using data in memory
    /// </summary>
    /// <param name="sourceData"></param>
    /// <param name="filePath">location of the path inside the container</param>
    void AddTextFile(string sourceData,
        string filePath);

    /// <summary>
    /// Checks entry existence
    /// </summary>
    /// <param name="filePath">location of the path inside the container</param>
    /// <returns>true if the entry exists</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// Read an entry in text format
    /// </summary>
    /// <param name="filePath">location of the path inside the container</param>
    /// <returns>the content of the entry</returns>
    string ReadTextFile(string filePath);

    /// <summary>
    /// Read an entry in binary format
    /// </summary>
    /// <param name="filePath">location of the path inside the container</param>
    /// <returns>the content of the entry</returns>
    byte[] ReadBinaryFile(string filePath);

    /// <summary>
    /// Gets the full list of files found inside the container
    /// </summary>
    /// <returns>the list of found files. The returned path is relative to the root folder</returns>
    /// <exception cref="AccessViolationException"></exception>
    string[] GetAllFiles();

    /// <summary>
    /// Gets the list of files and folders in data container
    /// </summary>
    /// <returns>A chained list of FolderContent</returns>
    public FolderContent GetAllFoldersAndContent();

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="filePath">location of the path inside the container</param>
    void DeleteFile(string filePath);

    /// <summary>
    /// Deletes an empty folder
    /// </summary>
    /// <param name="folderPath"></param>
    /// <inheritdoc cref="Directory.Delete(string)" />
    void DeleteFolder(string folderPath);

    /// <summary>
    /// Creates an empty folder
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="failIfExists"></param>
    /// <inheritdoc cref="Directory.CreateDirectory(string)" />
    void CreateFolder(string folderPath,
        bool failIfExists=false);
}