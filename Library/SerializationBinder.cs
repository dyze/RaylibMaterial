using System.Text;
using Newtonsoft.Json.Serialization;

namespace Library;

public class SerializationBinder : ISerializationBinder
{
    private readonly IList<Type> _knownTypes;

    public SerializationBinder(IList<Type> knowTypes)
    {
        _knownTypes = knowTypes;
    }

    public void BindToName(Type serializedType,
        out string assemblyName,
        out string typeName)
    {
        if (IsTypeAccepted(serializedType) == false)
            throw new TypeAccessException($"Serialization of type {serializedType} is not authorised");

        assemblyName = serializedType.Assembly.FullName ?? throw new NullReferenceException();
        typeName = serializedType.FullName ?? throw new NullReferenceException();
    }

    public Type BindToType(string? assemblyName,
        string typeName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
        if (assembly == null)
            throw new TypeAccessException($"Assembly {assemblyName} not found");

        var type = _knownTypes?.SingleOrDefault(t => t.Name == typeName
                                                 || t.FullName == typeName
                                                 || RemoveAssemblyDetails(t.FullName) == typeName);
        if (type != null)
            return type;

        // Look for types with Serializable flag
        var fullTypeName = $"{typeName}, {assemblyName}";

        type = Type.GetType(fullTypeName, false);
        if (type == null)
            throw new TypeAccessException($"Type {typeName} not found in assembly {assemblyName}");

        if (type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
            return type;

        throw new TypeAccessException($"Deserialization of type {typeName} is not authorised");
    }

    private bool IsTypeAccepted(Type type)
    {
        if (_knownTypes?.SingleOrDefault(t => t == type) != null)
            return true;

        if (type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
            return true;

        return false;
    }

    // Some types have a fullname that include the assembly version number, but json files doesn't have
    // Therefore the type can't be properly validated during deserialization
    // I fixed it by applying the algorithm used by Newtonsoft.Json during serialisation
    // Source Src/Newtonsoft.Json/Utilities/ReflectionUtils.cs
    private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
    {
        var stringBuilder = new StringBuilder();
        var flag = false;
        var flag2 = false;
        var flag3 = false;
        foreach (var c in fullyQualifiedTypeName)
        {
            switch (c)
            {
                case '[':
                    flag = false;
                    flag2 = false;
                    flag3 = true;
                    stringBuilder.Append(c);
                    break;
                case ']':
                    flag = false;
                    flag2 = false;
                    flag3 = false;
                    stringBuilder.Append(c);
                    break;
                case ',':
                    if (flag3)
                    {
                        stringBuilder.Append(c);
                    }
                    else if (!flag)
                    {
                        flag = true;
                        stringBuilder.Append(c);
                    }
                    else
                    {
                        flag2 = true;
                    }
                    break;
                default:
                    flag3 = false;
                    if (!flag2)
                    {
                        stringBuilder.Append(c);
                    }
                    break;
            }
        }
        return stringBuilder.ToString();
    }
}
