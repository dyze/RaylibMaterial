using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableVector3 : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Vector3 Value { get; set; }

}