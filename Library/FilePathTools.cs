namespace Library;

public static class FilePathTools
{
    public static string ExtractFolderFromFilePath(string filePath)
    {
        var index = filePath.LastIndexOf(Path.DirectorySeparatorChar);
        if (index < 0)
            return "";
        return filePath[..index];
    }

    public static string ExtractFileNameFromFilePath(string filePath)
    {
        var index = filePath.LastIndexOf(Path.DirectorySeparatorChar);
        if (index < 0)
            return "";
        return filePath[(index + 1)..];
    }

    /// <summary>
    /// Remove first directory of path (if one exists).
    /// e.g. "maps\\mymaps\\hehe.map" becomes "mymaps\\hehe.map"
    /// Also used to cut first folder off, especially useful for relative
    /// paths. e.g. "maps\\test" becomes "test"
    /// </summary>
    public static string RemoveFirstDirectory(string path)
    {
        var i = path.IndexOf("\\");
        if (i >= 0 && i < path.Length)
            // Return rest of path
            return path.Substring(i + 1);
        // No first directory found, just return original path
        return path;
    } 
}
