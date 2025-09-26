using Newtonsoft.Json;

namespace Editor.Configuration;


public class EditorConfigurationStorage
{
    private const string FileName = "editor.cfg";

    public static EditorConfiguration Load(string folderPath)
    {
        var json = File.ReadAllText(Path.Combine(folderPath, FileName));
        var config = JsonConvert.DeserializeObject<EditorConfiguration>(json);
        if (config == null)
            throw new FileLoadException($"{folderPath} is probably not a json file");
        return config;
    }

    public static void Save(EditorConfiguration config, string projectPath)
    {
        var json = JsonConvert.SerializeObject(config,
            Formatting.Indented);

        File.WriteAllText(Path.Combine(projectPath, FileName), 
            json);
    }
}