using Newtonsoft.Json;

namespace Editor.Configuration;


public class EditorConfiguration
{
    [JsonProperty("DataFileExplorer")] public DataFileExplorerConfiguration DataFileExplorerConfiguration = new();

    [JsonProperty("WorkspaceConfiguration")] public WorkspaceConfiguration WorkspaceConfiguration = new();

}
