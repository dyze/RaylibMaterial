using ImGuiNET;
using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Packaging;
using Color = System.Drawing.Color;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableColor : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Color Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, TypeConverters.ColorToVector4(Value), ShaderUniformDataType.Vec4);
    }
}