using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Packaging;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableUiVector2 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public readonly uint[] Value = new uint[2];

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.UIVec2);
    }
}