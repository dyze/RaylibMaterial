using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableVector2 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Vector2 Value { get; set; }

}