using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableColor : CodeVariableBase
{
    [Required][JsonProperty("Value")] public Color Value { get; set; }

}