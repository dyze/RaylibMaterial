using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableInt : CodeVariableBase
{
    [Required][JsonProperty("Value")] public int Value { get; set; }

}