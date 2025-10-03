using Newtonsoft.Json;
using System.Drawing;

namespace ConsumerSampleApp.Configuration;


public class Configuration
{
    [JsonProperty("WindowPosition")] public Point WindowPosition { get; set; } = new(40, 40);

    [JsonProperty("WindowSize")] public Size WindowSize = new(1600, 900);
    [JsonProperty("MonitorIndex")] public int MonitorIndex { get; set; } = 0;

    /// <summary>
    /// Path of folder containing resource data files
    /// </summary>
    [JsonProperty("ResourceFolderPath")] public string ResourceFolderPath;

    /// <summary>
    /// Path of folder containing material packages that have been created using the editor
    /// </summary>
    [JsonProperty("MaterialFolderPath")] public string MaterialFolderPath;
    
    public string ModelFolderPath => $"{ResourceFolderPath}/models";
}
