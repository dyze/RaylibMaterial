using System.ComponentModel.DataAnnotations;
using Library.Packaging;
using Newtonsoft.Json;
using Raylib_cs;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableFloat : CodeVariableBase
{
    [Required][JsonProperty("Value")] public float Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.Float);
    }
}
