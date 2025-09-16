namespace Library.Packaging;

public class FileSystemAccess : IDataContainerAccess
{
    private string? _rootFolder;
    private bool _isReadAllowed;
    private bool _isWriteAllowed;

    /// <inheritdoc />
    public void Open(string containerPath, 
        AccessMode accessMode)
    {
        Close();

        switch (accessMode)
        {
            case AccessMode.Read:
                _isReadAllowed = true;
                _isWriteAllowed = false;
                break;
            case AccessMode.Create:
                _isReadAllowed = false;
                _isWriteAllowed = true;
                break;
            case AccessMode.ReadWrite:
                _isReadAllowed = true;
                _isWriteAllowed = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(accessMode), accessMode, null);
        }

        if(_isWriteAllowed)
            Directory.CreateDirectory(containerPath);


        _rootFolder = containerPath;
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public bool Exists()
    {
        return Directory.Exists(_rootFolder);
    }

    /// <inheritdoc />
    public void AddFileUsingOtherFile(string sourceFilePath,
        string destinationFilePath)
    {
        if (_isWriteAllowed == false)
            throw new AccessViolationException("invalid mode set");

        var folderPath = FilePathTools.ExtractFolderFromFilePath(destinationFilePath);
        CreateFolder(folderPath);

        var inputData = File.ReadAllBytes(BuildPath(destinationFilePath));
        File.WriteAllBytes(BuildPath(destinationFilePath), inputData);
    }

    /// <inheritdoc />
    public void AddBinaryFile(byte[] sourceData,
        string filePath)
    {
        if (_isWriteAllowed == false)
            throw new AccessViolationException("invalid mode set");

        var folderPath = FilePathTools.ExtractFolderFromFilePath(filePath);
        CreateFolder(folderPath);

        var file = File.Open(BuildPath(filePath), 
            FileMode.Create);
        file.Write(sourceData);
        file.Close();
    }

    /// <inheritdoc />
    public void AddTextFile(string sourceData,
        string filePath)
    {
        if (_isWriteAllowed == false)
            throw new AccessViolationException("invalid mode set");

        var folderPath = FilePathTools.ExtractFolderFromFilePath(filePath);
        CreateFolder(folderPath);

        File.WriteAllText(BuildPath(filePath), sourceData);
    }

    /// <inheritdoc />
    public bool FileExists(string filePath)
    {
        return File.Exists(BuildPath(filePath));
    }

    private string BuildPath(string subPath)
    {
        if (_rootFolder == null)
            throw new NullReferenceException("_rootFolder can't be null");

        return Path.Combine(_rootFolder, subPath);
    }

    /// <inheritdoc />
    public string ReadTextFile(string filePath)
    {
        if (_isReadAllowed == false)
            throw new AccessViolationException("invalid mode set");

        return File.ReadAllText(BuildPath(filePath));
    }

    /// <inheritdoc />
    public byte[] ReadBinaryFile(string filePath)
    {
        if (_isReadAllowed == false)
            throw new AccessViolationException("invalid mode set");

        return File.ReadAllBytes(BuildPath(filePath));
    }

    /// <inheritdoc />
    public string[] GetAllFiles()
    {
        if (_isReadAllowed == false)
            throw new AccessViolationException("invalid mode set");

        var files = Directory.GetFiles(BuildPath(""), 
            "*.*", 
            SearchOption.AllDirectories);

        var outputFiles = new string[files.Length];

        var i = 0;
        foreach (var file in files)
            outputFiles[i++] = GetPathWithoutRoot(file);

        return outputFiles;
    }

    private string GetPathWithoutRoot(string fullPath)
    {
        if (_rootFolder == null)
            throw new NullReferenceException("_rootFolder can't be null");

        // Remove rootFolder
        var offset = _rootFolder.Length;
        if (fullPath[offset] == '\\')
            offset++;

        return fullPath[offset..];
    }

    /// <inheritdoc />
    public FolderContent GetAllFoldersAndContent()  
    {
        if (_rootFolder == null)
            throw new NullReferenceException("_rootFolder can't be null");

        var folderContent = new FolderContent(_rootFolder, "");
        GetAllFoldersAndContentInternal(BuildPath(""),
            ref folderContent);
        return folderContent;
    }

    private void GetAllFoldersAndContentInternal(string folderPath, 
        ref FolderContent folder)
    {
        var files = Directory.EnumerateFiles(folderPath);
        foreach (var file in files)
            folder.Files.Add(Path.GetFileName(file));

        var directories = Directory.EnumerateDirectories(folderPath);
        foreach (var directory in directories)
        {
            var folderName = Path.GetFileName(directory);
            var subFolder = new FolderContent(directory, GetPathWithoutRoot(directory));
            folder.Folders.Add(folderName, subFolder);
            GetAllFoldersAndContentInternal(directory,
                ref subFolder);
        }
    }

    /// <inheritdoc />
    public void DeleteFile(string filePath)
    {
        if (_isWriteAllowed == false)
            throw new AccessViolationException("invalid mode set");

        if (File.Exists(BuildPath(filePath)) == false)
            throw new FileNotFoundException(BuildPath(filePath));

        File.Delete(BuildPath(filePath));
    }

    /// <inheritdoc />
    public void DeleteFolder(string folderPath)
    {
        if (_isWriteAllowed == false)
            throw new AccessViolationException("invalid mode set");

        if (Directory.Exists(BuildPath(folderPath)) == false)
            throw new DirectoryNotFoundException(BuildPath(folderPath));

        Directory.Delete(BuildPath(folderPath));
    }

    /// <inheritdoc />
    public void CreateFolder(string folderPath,
        bool failIfExists = false)
    {
        if (_isWriteAllowed == false)
            throw new AccessViolationException("invalid mode set");

        var exists = Directory.Exists(BuildPath(folderPath));

        if (exists)
        {
            if (failIfExists)
                throw new Exception($"{folderPath} already exists");
            else
                return;
        }

        Directory.CreateDirectory(BuildPath(folderPath));
    }
}