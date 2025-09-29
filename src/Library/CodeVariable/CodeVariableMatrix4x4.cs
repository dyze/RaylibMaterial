using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableMatrix4x4 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Matrix4x4 Value { get; set; }

}