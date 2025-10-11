using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Packaging;

using IVector3 = Library.Types.GenericVector3<int>;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableIVector3 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public IVector3 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.IVec3);
    }
}