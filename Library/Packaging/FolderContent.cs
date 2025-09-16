namespace Library.Packaging;

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