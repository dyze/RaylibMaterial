using System.ComponentModel.DataAnnotations;

namespace Library.Packaging;

[Serializable]
public class MaterialDescription
{
    [Required] public string Description = "";
    [Required] public string Author = "";
    [Required] public List<string> Tags = [];

    /// <summary>
    /// Package might contain sub shaders.
    /// ShaderNames identify the main shaders to apply
    /// </summary>
    internal Dictionary<FileType, string> ShaderNames = [];

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