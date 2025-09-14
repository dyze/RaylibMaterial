using Newtonsoft.Json;

namespace Library;

public class MaterialStorage
{
    public static Material Load(string filePath)
    {
        var json = File.ReadAllText(filePath);

        var configuration = ParseJson(json);

        return configuration;
    }

    public static Material ParseJson(string json)
    {
        JsonSerializerSettings jsonDeserializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = new SerializationBinder(PayloadValidator.GetAllowedPayloadTypes()),
        };

        var content = JsonConvert.DeserializeObject<Material>(json,
                          jsonDeserializerSettings) ??
                      throw new ApplicationException("json can't be deserialized");

        if (content == null)
            throw new InvalidDataException("invalid file");

        return content;
    }

    public static string ToJson(Material projectConfiguration)
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

    public static void Save(Material material,
        string filePath)
    {
        var json = ToJson(material);

        var directionPath = Path.GetDirectoryName(filePath);
        if (directionPath == null)
            throw new DirectoryNotFoundException($"{directionPath} is not valid");
        Directory.CreateDirectory(directionPath);

        File.WriteAllText(filePath, json);

        material.FullFilePath = Path.GetFullPath(filePath);
        material.IsModified = false;
    }
}