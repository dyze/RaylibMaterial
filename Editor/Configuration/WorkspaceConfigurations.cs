using Newtonsoft.Json;

namespace Editor.Configuration;

public class WorkspaceConfiguration
{
    [JsonProperty("DataFileExplorerIsVisible")] public bool DataFileExplorerIsVisible = true;
    [JsonProperty("MessageWindowIsVisible")] public bool MessageWindowIsVisible = true;
}
