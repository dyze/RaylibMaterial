namespace Library.Packaging;

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