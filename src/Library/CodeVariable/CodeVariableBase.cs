using System.Text.Json.Serialization;

namespace Library.CodeVariable;

[Serializable]
public abstract class CodeVariableBase
{
    /// <summary>
    /// is true if handled by Raylib or MaterialPackage
    /// such variable can't be modified by user
    /// </summary>
    public bool Internal = false;

    /// <summary>
    /// is true when value needs to be sent to shader
    /// </summary>
    [JsonIgnore]
    public bool SendToShader = true;

    public override string ToString()
    {
        return $"Internal={Internal}, SendToShader={SendToShader}";
    }
}