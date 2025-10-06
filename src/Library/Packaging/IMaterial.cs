namespace Library.Packaging;

public interface IMaterial
{
    public byte[] GetFile(FileType fileType, string fileName);
}