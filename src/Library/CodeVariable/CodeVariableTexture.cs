using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableTexture : CodeVariableBase
{
    [Required][JsonProperty("Value")] public string Value { get; set; } = "";

}