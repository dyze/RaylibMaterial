using Library.Packaging;

namespace Editor.Windows;

public class DataFileExplorerData
{
    public string SelectedFolder { get; set; } = "";
    public string DraggedFile { get; set; } = "";

    public FileSystemAccess DataFolder { get; set; }

    public FolderContent? DataRootFolder { get; private set; }

    public void RefreshDataRootFolder()
    {
        DataRootFolder = DataFolder.GetAllFoldersAndContent();
    }
}