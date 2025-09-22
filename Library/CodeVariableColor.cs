using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Library;

[Serializable]
public class CodeVariableColor : CodeVariable
{
    [Required][JsonProperty("Value")] public Color Value { get; set; }

}