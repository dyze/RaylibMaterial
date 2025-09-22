using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Library;

[Serializable]
public class CodeVariableTexture : CodeVariable
{
    [Required][JsonProperty("Value")] public string Value { get; set; } = "";

}