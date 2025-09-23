using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableFloat : CodeVariableBase
{
    [Required][JsonProperty("Value")] public float Value { get; set; }

}