using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Library.Packaging;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableIVector3 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public readonly int[] Value = new int[3];

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.IVec3);
    }
}