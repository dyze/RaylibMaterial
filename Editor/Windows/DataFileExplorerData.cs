using Library.Packaging;

namespace Editor.Windows;

public class DataFileExplorerData
{
    public string SelectedFolder { get; set; } = "";

    public string DraggedFullFilePath{ get; set; } = "";
    public string DraggedFileName { get; set; } = "";

    public FileSystemAccess DataFolder { get; set; }

    public FolderContent? DataRootFolder { get; private set; }

    public void RefreshDataRootFolder()
    {
        DataRootFolder = DataFolder.GetAllFoldersAndContent();
    }
}