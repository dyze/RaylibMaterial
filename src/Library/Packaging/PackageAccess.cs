using Library.Helpers;
using System.Data;
using System.IO.Compression;
using System.Text;

namespace Library.Packaging;

/// <summary>
/// A package is a single file containing a tree of files
/// </summary>
public class PackageAccess : IDisposable, IDataContainerAccess
{
    private string? _filePath;
    private FileStream? _zipStream;
    private ZipArchive? _zipArchive;

    private AccessMode? _mode;

    public void Dispose()
    {
        _zipArchive?.Dispose();
        _zipStream?.Dispose();
    }

    /// <inheritdoc />
    public void Open(string containerPath,
        AccessMode accessMode)
    {
        Close();

        FileMode fileMode;
        ZipArchiveMode archiveMode;
        switch (accessMode)
        {
            case AccessMode.Read:
                fileMode = FileMode.Open;
                archiveMode = ZipArchiveMode.Read;
                break;
            case AccessMode.Create:
                fileMode = FileMode.Create;
                archiveMode = ZipArchiveMode.Create;

                var folderPath = FilePathTools.ExtractFolderFromFilePath(containerPath);
                if(folderPath != "")
                    Directory.CreateDirectory(folderPath);

                break;
            case AccessMode.ReadWrite:
                throw new ArgumentOutOfRangeException($"{nameof(accessMode)} accessMode is not supported for Packages");
            default:
                throw new ArgumentOutOfRangeException(nameof(accessMode), accessMode, null);
        }

        _zipStream = new FileStream(containerPath, fileMode);
        _zipArchive = new ZipArchive(_zipStream, archiveMode, true);

        _filePath = containerPath;
        _mode = accessMode;
    }

    /// <inheritdoc />
    public void Close()
    {
        Dispose();
        _mode = null;
    }

    /// <inheritdoc />
    public void AddFileUsingOtherFile(string sourceFilePath, string destinationFilePath)
    {
        if (_mode != AccessMode.Create)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        _zipArchive.CreateEntryFromFile(sourceFilePath, destinationFilePath);
    }

    /// <inheritdoc />
    public void AddBinaryFile(byte[] sourceData, 
        string filePath)
    {
        if (_mode != AccessMode.Create)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        var entry = _zipArchive.CreateEntry(filePath);
        using (var entryStream = entry.Open())
        {
            entryStream.Write(sourceData, 0, sourceData.Length);
        }
    }

    /// <inheritdoc />
    public void AddTextFile(string sourceData, string filePath)
    {
        if (_mode != AccessMode.Create)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        var entry = _zipArchive.CreateEntry(filePath);
        using (var entryStream = entry.Open())
        {
            byte[] bytes = Encoding.ASCII.GetBytes(sourceData);
            entryStream.Write(bytes, 0, bytes.Length);
        }
    }

    /// <inheritdoc />
    public byte[] ReadBinaryFile(string filePath)
    {
        if (_mode != AccessMode.Read)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        var entry = _zipArchive.GetEntry(filePath);
        if (entry == null)
            throw new FileNotFoundException("entry not found in archive");

        var length = entry.Length;

        var output = new byte[length];

        var totalRead = 0;

        using var entryStream = entry.Open();
        using var streamReader = new BinaryReader(entryStream);

        // all data is not always available during the first Read
        var readBytes = 0;
        do
        {
            readBytes = streamReader.Read(output, totalRead, output.Length - totalRead);
            totalRead += readBytes;
        } while (readBytes > 0 && totalRead < output.Length);

        if (totalRead != entry.Length)
            throw new DataException("size mismatch");

        return output;
    }

    /// <inheritdoc />
    public string ReadTextFile(string filePath)
    {
        if (_mode != AccessMode.Read)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        var entry = _zipArchive.GetEntry(filePath);

        if (entry == null)
            throw new NullReferenceException("entry is null");

        using var entryStream = entry.Open();
        using var streamReader = new StreamReader(entryStream);
        var output = streamReader.ReadToEnd();

        return output;
    }

    /// <inheritdoc />
    public string[] GetAllFiles()
    {
        if (_mode != AccessMode.Read)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        var files = new string[_zipArchive.Entries.Count];

        var index = 0;

        foreach (var entry in _zipArchive.Entries)
            files[index++] = entry.FullName;

        return files;
    }

    public FolderContent GetAllFoldersAndContent()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void DeleteFile(string filePath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void DeleteFolder(string folderPath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void CreateFolder(string folderPath,
        bool failIfExists = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    /// <inheritdoc />
    public bool FileExists(string filePath)
    {
        if (_mode != AccessMode.Read)
            throw new AccessViolationException("invalid mode set");

        if (_zipArchive == null)
            throw new NullReferenceException("_zipArchive is null");

        return _zipArchive.GetEntry(filePath) != null;
    }

}