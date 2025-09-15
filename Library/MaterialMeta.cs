using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Library;

[Serializable]
public class MaterialMeta
{
    [JsonIgnore] public string _fileName = "no name";

    [JsonIgnore]
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            FullFilePath = "not saved yet";
        }
    }

    [JsonIgnore] public string FullFilePath = "not saved yet";

    [Required] [JsonProperty("Description")] public string Description = "?";

    [Required] [JsonProperty("Author")] public string Author = "?";
    [Required] [JsonProperty("Tags")] public List<string> Tags = [];

    [Required][JsonProperty("Variables")] public Dictionary<string, CodeVariable> Variables = [];

    [JsonIgnore] public bool IsModified { get; set; } = true;

    public void SetModified() => IsModified = true;
}