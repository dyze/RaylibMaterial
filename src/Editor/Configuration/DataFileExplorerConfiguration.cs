using Newtonsoft.Json;

namespace Editor.Configuration;

public class DataFileExplorerConfiguration
{
    [JsonProperty("ResourcesPath")] public string ResourcesPath;

    [JsonProperty("FavouriteDirectories")] public List<string> FavouriteDirectories = [];
    [JsonIgnore] public const int MaxFavouriteDirectories = 5;

    public void AddToFavourite(string path)
    {
        if (FavouriteDirectories.Contains(path))
            return;

        if (FavouriteDirectories.Count >= MaxFavouriteDirectories)
            return;

        FavouriteDirectories.Add(path);
    }

    public void RemoveFavourite(int index)
    {
        if (index < 1)        // 0=editor resource path
            return;

        FavouriteDirectories.RemoveAt(index);
    }

    /// <summary>
    /// Default editor.cfg file contains relative paths
    /// This function converts them to full paths. Such conversion simplifies readability for user
    /// </summary>
    public void SolveFullPaths()
    {
        for (var index = 0; index < FavouriteDirectories.Count; index++)
        {
            var editorConfigurationFavouriteDirectory = FavouriteDirectories[index];
            editorConfigurationFavouriteDirectory = Path.GetFullPath(editorConfigurationFavouriteDirectory);
            FavouriteDirectories[index] = editorConfigurationFavouriteDirectory;
        }

    }
}