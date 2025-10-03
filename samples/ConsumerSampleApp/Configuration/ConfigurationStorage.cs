using Newtonsoft.Json;

namespace ConsumerSampleApp.Configuration;


public class ConfigurationStorage
{
    private const string FileName = "ConsumerSampleApp.cfg";

    public static Configuration Load(string folderPath)
    {
        var json = File.ReadAllText(Path.Combine(folderPath, FileName));
        var config = JsonConvert.DeserializeObject<Configuration>(json);
        if (config == null)
            throw new FileLoadException($"{folderPath} is probably not a json file");
        return config;
    }

    public static void Save(Configuration config, string projectPath)
    {
        var json = JsonConvert.SerializeObject(config,
            Formatting.Indented);

        File.WriteAllText(Path.Combine(projectPath, FileName), 
            json);
    }
}