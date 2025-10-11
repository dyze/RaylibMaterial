using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Packaging;

using UVector3 = Library.Types.GenericVector3<uint>;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableUVector3 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public UVector3 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.UIVec3);
    }
}