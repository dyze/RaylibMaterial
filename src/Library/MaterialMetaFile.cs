using Library.CodeVariable;
using Library.Packaging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Library;

[Serializable]
public class MaterialMetaFile
{
    public static readonly Version CurrentVersion = new Version(0, 1);

    [Required][JsonProperty("Version")] public Version Version = CurrentVersion;
    [Required][JsonProperty("Description")] public string Description = "";

    [Required][JsonProperty("Author")] public string Author = "";
    [Required][JsonProperty("Tags")] public List<string> Tags = [];

    [Required][JsonProperty("Variables")] public Dictionary<string, CodeVariableBase> Variables = [];

    /// <summary>
    /// Names of main shaders to apply
    /// </summary>
    [JsonProperty("ShaderNames")] internal Dictionary<FileType, string> ShaderNames = [];
}
