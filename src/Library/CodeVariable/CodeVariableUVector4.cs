using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Packaging;

using UVector4 = Library.Types.GenericVector4<uint>;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableUVector4 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public UVector4 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.UIVec4);
    }
}