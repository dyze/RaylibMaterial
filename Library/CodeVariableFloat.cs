using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Library;

[Serializable]
public class CodeVariableFloat : CodeVariable
{
    [Required][JsonProperty("Value")] public float Value { get; set; }

}