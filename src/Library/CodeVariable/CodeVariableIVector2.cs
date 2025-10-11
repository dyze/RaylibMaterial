using Library.Packaging;
using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;

using IVector2 = Library.Types.GenericVector2<int>;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableIVector2 : CodeVariableBase
{
    [Required] [JsonProperty("Value")] public IVector2 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.IVec2);
    }
}