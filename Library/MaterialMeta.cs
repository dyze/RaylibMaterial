using Library.CodeVariable;
using Library.Packaging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Library;

[Serializable]
public class MaterialMeta
{
    [JsonIgnore] public string _fileName = "no name";

    [JsonIgnore]
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            FullFilePath = "not saved yet";
        }
    }

    [JsonIgnore] public string FullFilePath = "not saved yet";

    [Required] [JsonProperty("Description")] public string Description = "?";

    [Required] [JsonProperty("Author")] public string Author = "?";
    [Required] [JsonProperty("Tags")] public List<string> Tags = [];

    [Required][JsonProperty("Variables")] public Dictionary<string, CodeVariableBase> Variables = [];

    [JsonIgnore] public bool IsModified { get; set; } = true;

    public void SetModified() => IsModified = true;

    public void TriggerVariablesChanged() => OnVariablesChanged?.Invoke();

    public event Action? OnVariablesChanged;


    /// <summary>
    /// Names of main shaders to apply
    /// </summary>
    [JsonProperty("ShaderNames")]
    private Dictionary<FileType, string> ShaderNames = [];

    public void SetShaderName(FileType shaderType, string shaderName)
    {
        ShaderNames[shaderType] = shaderName;
    }

    public string? GetShaderName(FileType shaderType)
    {
        ShaderNames.TryGetValue(shaderType, out var shaderName);
        return shaderName;
    }
}