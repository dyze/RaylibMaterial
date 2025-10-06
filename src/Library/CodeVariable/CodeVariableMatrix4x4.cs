using Library.Packaging;
using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using NLog;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableMatrix4x4 : CodeVariableBase
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Required][JsonProperty("Value")] public Matrix4x4 Value { get; set; }

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Logger.Error($"{nameof(Matrix4x4)} not supported");
    }
}