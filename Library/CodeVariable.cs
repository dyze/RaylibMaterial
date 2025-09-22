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

    [Required][JsonProperty("Value")] public object? Value { get; set; }


    public CodeVariable(Type type)
    {
        _type = type;   // just to avoid warning
        Type = type;
    }

    public static object? GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            var value = Activator.CreateInstance(type);
            if (value == null)
                throw new NullReferenceException("value can't be null");
            return value;
        }

        return null;
    }
}