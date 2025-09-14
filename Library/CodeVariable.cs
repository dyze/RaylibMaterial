namespace Library;

public class CodeVariable
{
    public Type Type;
    public string Name;

    public CodeVariable(Type type, string name)
    {
        Type = type;
        Name = name;

        if (type == typeof(string))
            Value = "";
        else
            Value = GetDefault(type);
    }

    public object Value { get; set; }

    public static object GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}