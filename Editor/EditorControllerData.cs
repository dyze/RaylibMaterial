using Editor.Windows;
using Library.Packaging;

namespace Editor;

public class EditorControllerData
{
    public DataFileExplorerData DataFileExplorerData { get; set; } = new();

    public MaterialPackage MaterialPackage;


    public EditorControllerData()
    {
    }
}