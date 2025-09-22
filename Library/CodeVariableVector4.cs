using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Library;

[Serializable]
public class CodeVariableVector4 : CodeVariable
{
    [Required][JsonProperty("Value")] public Vector4 Value { get; set; }

}