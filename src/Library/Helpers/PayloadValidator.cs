using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;

namespace Library.Helpers;

public class PayloadValidator
{
    public static List<Type> GetAllowedPayloadTypes()
    {
        var allTypes = new List<Type>();

        allTypes.AddRange([
            typeof(Vector4)
        ]);

        var types2 = GetTypesHavingJsonPropertyAttribute();
        foreach (var t2 in types2)
        {
            if (allTypes.Exists(p => p.FullName == t2.FullName) == false)
                allTypes.Add(t2);
        }

        return allTypes;
    }

    public static HashSet<Type> GetTypesHavingJsonPropertyAttribute()
    {
        HashSet<Type> returnedTypes = [];

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                // List fields with JsonPropertyAttribute
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f => Attribute.IsDefined(f, typeof(JsonPropertyAttribute)));
                var childrenTypes = fields.Select(field => field.FieldType).ToHashSet();

                // Add properties with JsonPropertyAttribute
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f => Attribute.IsDefined(f, typeof(JsonPropertyAttribute)));
                childrenTypes.UnionWith(properties.Select(field => field.PropertyType));

                var fieldWithAttributeFound = false;

                foreach (var childType in childrenTypes)
                {
                    fieldWithAttributeFound = true;
                    returnedTypes.Add(childType);

                    // Consider types of elements for generics (e.g. List<>)
                    var elementTypes = childType.GetGenericArguments();
                    foreach (var elementType in elementTypes)
                    {
                        returnedTypes.Add(elementType);
                    }
                }

                if (fieldWithAttributeFound)
                    // Add the type of class/struct containing the field
                    returnedTypes.Add(type);
            }
        }

        return returnedTypes;
    }

}
