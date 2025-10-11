using Library.Packaging;
using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using IVector4 = Library.Types.GenericVector4<int>;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableIVector4 : CodeVariableBase
{
    [Required] [JsonProperty("Value")] public IVector4 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.IVec4);
    }
}