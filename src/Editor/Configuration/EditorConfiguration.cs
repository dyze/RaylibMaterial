using Newtonsoft.Json;

namespace Editor.Configuration;


public class EditorConfiguration
{
    [JsonProperty("DataFileExplorer")] public DataFileExplorerConfiguration DataFileExplorerConfiguration = new();

    [JsonProperty("WorkspaceConfiguration")] public WorkspaceConfiguration WorkspaceConfiguration = new();

    [JsonProperty("RecentFiles")] public List<string> RecentFiles = [];

    private const int MaxRecentFiles = 5;

    public void AddRecentFile(string filePath)
    {
        // Recent files at the top

        var index = RecentFiles.FindIndex(f => f == filePath);
        if (index >= 0)
        {
            RecentFiles.RemoveAt(index);
            RecentFiles.Insert(0, filePath);
            return;
        }

        if (RecentFiles.Count >= MaxRecentFiles)
        {
            var startIndex = MaxRecentFiles - 1;
            var count = RecentFiles.Count - startIndex;
            RecentFiles.RemoveRange(startIndex, count);
        }
        RecentFiles.Insert(0, filePath);
    }
}
