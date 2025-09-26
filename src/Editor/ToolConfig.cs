using Raylib_cs;

namespace Editor;

class ToolConfig
{
    public string Name { get; set; }
    public string ImageFileName;
    public Texture2D Texture;

    public ToolConfig(string name,
        string imageFileName)
    {
        ImageFileName = imageFileName;
        Name = name;
    }
}