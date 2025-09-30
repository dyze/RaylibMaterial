using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableTexture : CodeVariableBase
{
    [Required][JsonProperty("Value")] public string Value { get; set; } = "";

}