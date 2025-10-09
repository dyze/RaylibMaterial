using Raylib_cs;

namespace Editor.Ui;

public class SkyBoxConfig
{
    public string Name { get; set; }
    public string? ImageFileName;
    public Texture2D Texture;

    public SkyBoxConfig(string name,
        string? imageFileName)
    {
        ImageFileName = imageFileName;
        Name = name;
    }
}