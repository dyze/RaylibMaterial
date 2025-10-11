using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Packaging;

using UVector2 = Library.Types.GenericVector2<uint>;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableUVector2 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public UVector2 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.UIVec2);
    }
}