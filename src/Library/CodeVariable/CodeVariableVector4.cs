using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableVector4 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Vector4 Value { get; set; }

}