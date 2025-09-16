using System.Diagnostics;
using System.Numerics;
using Editor.Configuration;
using Editor.Processes;
using ImGuiNET;
using Library.Packaging;
using NLog;
using Raylib_cs;

namespace Editor.Windows;

public class DataFileExplorer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly EditorConfiguration _editorConfiguration;
    private readonly DataFileExplorerData _dataFileExplorerData;
    private readonly DataFileExplorerConfiguration _dataFileExplorerConfiguration;

    private FolderContent? _selectedFolder;

    /// <summary>
    /// Active process. not null if the process is in progress 
    /// </summary>
    private EditorProcess? _activeProcess;

    private readonly Dictionary<string, Action<DataFileExplorer>> _mainActions = new()
    {
        { "refresh", (renderer => renderer.OnRefresh()) },
    };

    private readonly Dictionary<string, Action<DataFileExplorer, FolderContent>> _folderActions = new()
    {
        { "explore folder", (_, folder) => Process.Start("explorer.exe", folder.FullPath) },
    };

    public DataFileExplorer(EditorConfiguration editorConfiguration, 
        DataFileExplorerData dataFileExplorerData)
    {
        _editorConfiguration = editorConfiguration;
        _dataFileExplorerData = dataFileExplorerData;
        _dataFileExplorerConfiguration = editorConfiguration.DataFileExplorerConfiguration;
    }

    public void Render()
    {
        RenderInternal();
    }

    private void RenderInternal()
    {
        var size = new Vector2(400,
            (float)Raylib.GetScreenHeight() / 3);
        var position = new Vector2(Raylib.GetScreenWidth() - size.X,
            (float)2 * Raylib.GetScreenHeight() / 3);

        ImGui.SetNextWindowPos(position, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Data file explorer", 
                ref _editorConfiguration.WorkspaceConfiguration.DataFileExplorerIsVisible))
        {
            RenderMainActions();

            if (ImGui.BeginChild("Folders"))
            {
                var rootFolder = _dataFileExplorerData.DataRootFolder;
                if (rootFolder == null)
                    throw new NullReferenceException("rootFolder is null");

                RenderFolderContent(rootFolder.RelativePath,
                    rootFolder);

                if (_selectedFolder != null)
                    _dataFileExplorerData.SelectedFolder = _selectedFolder.RelativePath;
                else
                    _dataFileExplorerData.SelectedFolder = "";

                ImGui.EndChild();
            }

            RenderActiveProcess();
        }

        ImGui.End();
    }


    private void RenderFolderContent(string name,
        FolderContent folderContent)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow;

        if (_dataFileExplorerConfiguration.IsFolderOpen(folderContent.RelativePath))
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);

        if (_selectedFolder == folderContent)
            flags |= ImGuiTreeNodeFlags.Selected;

        var openFolder = ImGui.TreeNodeEx(name.Length == 0 ? "\\" : name,
            flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
            _selectedFolder = folderContent;

        if (ImGui.BeginPopupContextItem())
        {
            RenderFolderActions(folderContent);
            ImGui.EndPopup();
        }

        _dataFileExplorerConfiguration.AddRemoveOpenFolder(folderContent.RelativePath,
            openFolder);

        if (openFolder)
        {
            foreach (var (subName, folder) in folderContent.Folders)
                RenderFolderContent(subName, folder);

            foreach (var file in folderContent.Files)
            {
                //var isAssetFile = Path.GetExtension(file) == ".asset";

                var open = ImGui.TreeNodeEx(file,
                    ImGuiTreeNodeFlags.Leaf);


                //if (isAssetFile)
                //{
                //_dataFileExplorerData.DraggedFile = "";
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    if (_dataFileExplorerData.DraggedFile == "")
                        Logger.Trace("Begin drag");

                    _dataFileExplorerData.DraggedFile = file;
                    //ImGui.SetDragDropPayload("prefab", IntPtr.Zero , 0);
                    unsafe
                    {
                        var i = 1;
                        int* tesnum = &i;
                        ImGui.SetDragDropPayload(DragDropItemIdentifiers.File, new IntPtr(tesnum), sizeof(int));
                    }

                    ImGui.Text($"{file}");

                    ImGui.EndDragDropSource();
                }
                //}

                if (ImGui.BeginPopupContextItem())
                {
                    //if (isAssetFile)
                    //{
                    //    var assetName = Path.GetFileNameWithoutExtension(file);
                    //    var asset = _engine.DataFiles.AssetContainer.GetAssetByName(assetName);

                    //    //if (asset == null)
                    //    //    Logger.Error($"no asset found with this name {assetName}");
                    //    RenderAssetActions(asset.Value.AssetFile.Id);
                    //}

                    ImGui.EndPopup();
                }

                if (open)
                    ImGui.TreePop();
            }

            ImGui.TreePop();
        }
    }

    private void RenderMainActions()
    {
        var first = true;
        foreach (var (key, action) in _mainActions)
        {
            if (first)
                ImGui.SameLine();

            if (ImGui.Button(key))
                action(this);

            first = false;
        }

        ImGui.Separator();
    }

    private void RenderFolderActions(FolderContent folder)
    {
        foreach (var (key, action) in _folderActions)
        {
            if (ImGui.Selectable(key))
                action(this, folder);
        }
    }


    private void RenderActiveProcess()
    {
        if (_activeProcess != null)
        {
            if (_activeProcess.Render())
            {
                _activeProcess = null;
            }
        }
    }

    private void OnRefresh()
    {
        _dataFileExplorerData.RefreshDataRootFolder();
    }

}