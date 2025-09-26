using Raylib_cs;

namespace Editor;

public class BackgroundConfig
{
    public string Name { get; set; }
    public string? ImageFileName;
    public Texture2D Texture;

    public BackgroundConfig(string name,
        string? imageFileName)
    {
        ImageFileName = imageFileName;
        Name = name;
    }
}