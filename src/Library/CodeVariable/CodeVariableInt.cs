using Library.Packaging;
using Newtonsoft.Json;
using NLog;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableInt : CodeVariableBase
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Required][JsonProperty("Value")] public int Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.Int);
    }
}