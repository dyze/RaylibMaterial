using Library.CodeVariable;
using Library.Packaging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Library;

[Serializable]
public class MaterialMeta
{
    [Required] [JsonProperty("Description")] public string Description = "";

    [Required] [JsonProperty("Author")] public string Author = "";
    [Required] [JsonProperty("Tags")] public List<string> Tags = [];

    [Required][JsonProperty("Variables")] public Dictionary<string, CodeVariableBase> Variables = [];

    [JsonIgnore] public bool IsModified { get; set; } = false;

    public void SetModified() => IsModified = true;

    public void TriggerVariablesChanged() => OnVariablesChanged?.Invoke();

    public event Action? OnVariablesChanged;


    /// <summary>
    /// Names of main shaders to apply
    /// </summary>
    [JsonProperty("ShaderNames")]
    private Dictionary<FileType, string> _shaderNames = [];

    public void SetShaderName(FileType shaderType, string shaderName)
    {
        _shaderNames[shaderType] = shaderName;
    }

    public string? GetShaderName(FileType shaderType)
    {
        _shaderNames.TryGetValue(shaderType, out var shaderName);
        return shaderName;
    }

    public void ClearModified() => IsModified = false;
}