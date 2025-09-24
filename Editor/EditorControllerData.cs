using Editor.Windows;
using Library.Packaging;

namespace Editor;

public class EditorControllerData
{
    public DataFileExplorerData DataFileExplorerData { get; set; } = new();
    

    public MaterialPackage MaterialPackage = new();

    /// <summary>
    /// null if new material
    /// </summary>
    public string? MaterialFilePath { get; set; }

    public EditorControllerData()
    {
    }
}