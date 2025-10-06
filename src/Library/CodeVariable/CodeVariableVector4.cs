using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Library.Packaging;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableVector4 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Vector4 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.Vec4);
    }
}