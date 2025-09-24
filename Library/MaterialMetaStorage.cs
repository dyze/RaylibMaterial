using Library.Helpers;
using Newtonsoft.Json;

namespace Library;

public class MaterialMetaStorage
{
    public static MaterialMeta Load(string filePath)
    {
        var json = File.ReadAllText(filePath);

        var configuration = ParseJson(json);

        return configuration;
    }

    public static MaterialMeta ParseJson(string json)
    {
        JsonSerializerSettings jsonDeserializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = new SerializationBinder(PayloadValidator.GetAllowedPayloadTypes()),
        };

        var content = JsonConvert.DeserializeObject<MaterialMeta>(json,
                          jsonDeserializerSettings) ??
                      throw new ApplicationException("json can't be deserialized");

        if (content == null)
            throw new InvalidDataException("invalid file");

        return content;
    }

    public static string ToJson(MaterialMeta projectConfiguration)
    {
        JsonSerializerSettings jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = new SerializationBinder(PayloadValidator.GetAllowedPayloadTypes())
        };

        return JsonConvert.SerializeObject(projectConfiguration,
            jsonSerializerSettings);
    }

    public static void Save(MaterialMeta materialMeta,
        string filePath)
    {
        var json = ToJson(materialMeta);

        var directionPath = Path.GetDirectoryName(filePath);
        if (directionPath == null)
            throw new DirectoryNotFoundException($"{directionPath} is not valid");
        Directory.CreateDirectory(directionPath);

        File.WriteAllText(filePath, json);

        materialMeta.IsModified = false;
    }
}