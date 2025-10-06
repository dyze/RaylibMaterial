using Raylib_cs;
using System.Text.Json.Serialization;
using Library.Packaging;

namespace Library.CodeVariable;

[Serializable]
public abstract class CodeVariableBase
{
    /// <summary>
    /// is true when value needs to be sent to shader
    /// </summary>
    [JsonIgnore]
    public bool SendToShader = true;

    public override string ToString()
    {
        return $"SendToShader={SendToShader}";
    }

    public abstract void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation);
}