using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Library;

[Serializable]
public class CodeVariable
{
    [JsonIgnore] private Type _type;

    [Required] [JsonProperty("Type")] public Type Type
    {
        get => _type;
        set
        {
            // reset value
            _type = value;
            if (_type == typeof(string))
                Value = "";
            else
                Value = GetDefault(_type);
        }
    }

    [Required] [JsonProperty("Value")] public object Value { get; set; }


    public CodeVariable(Type type)
    {
        Type = type;
    }

    public static object GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}