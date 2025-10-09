using Raylib_cs;

namespace Editor.Ui;

public class ToolConfig(
    string name,
    string imageFileName)
{
    public string Name { get; set; } = name;
    public string ImageFileName = imageFileName;
    public Texture2D Texture;
}