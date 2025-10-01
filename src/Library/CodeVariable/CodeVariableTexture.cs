using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableTexture : CodeVariableBase
{
    [Required][JsonProperty("Value")] public string Value { get; set; } = "";
    [Required][JsonProperty("MaterialMapIndex")] public MaterialMapIndex? MaterialMapIndex { get; set; }
}