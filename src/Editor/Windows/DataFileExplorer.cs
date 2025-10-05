using System.Diagnostics;
using System.Numerics;
using Editor.Configuration;
using Editor.Helpers;
using Editor.Processes;
using ImGuiNET;
using Library.Packaging;
using NLog;
using Raylib_cs;
using rlImGui_cs;

namespace Editor.Windows;

public class DataFileExplorer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly EditorConfiguration _editorConfiguration;
    private readonly DataFileExplorerData _dataFileExplorerData;
    private readonly DataFileExplorerConfiguration _dataFileExplorerConfiguration;

    private FolderContent? _selectedFolder;

    private readonly Dictionary<string, Texture2D> _fileTypeTextures = [];

    private readonly Dictionary<string, string> _textureNames = new()
    {
        { "folder", "64px-Icons8_flat_folder.png" },
        { "?", "64px-Orange_question_mark.png" },
        { ".vert", "shader-64px.png" },
        { ".frag", "shader-64px.png" },
        { ".png", "64px-Icons8_flat_picture.png" },
        { ".jpg", "64px-Icons8_flat_picture.png" },
        { ".txt", "64px-Text-txt.png" },
        { ".md", "64px-Text-txt.png" }
    };


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
        { "explore folder", (_, folder) => Process.Start("explorer.exe", Path.GetFullPath(folder.FullPath)) },
    };

    private readonly EditorControllerData _editorControllerData;

    public Action<string>? ImageOpenRequest;

    public DataFileExplorer(EditorConfiguration editorConfiguration,
        EditorControllerData editorControllerData,
        DataFileExplorerData dataFileExplorerData)
    {
        _editorConfiguration = editorConfiguration;
        _editorControllerData = editorControllerData;
        _dataFileExplorerData = dataFileExplorerData;
        _dataFileExplorerConfiguration = editorConfiguration.DataFileExplorerConfiguration;
    }

    public void PrepareUi()
    {
        foreach (var (key, filename) in _textureNames)
        {
            var image = Raylib.LoadImage(
                $"{_editorConfiguration.ResourceUiPath}/file-types/{filename}"); // ignore period
            if (Raylib.IsImageValid(image) == false)
            {
                Logger.Debug($"image {filename} is not valid");
                continue;
            }

            var textureImage = Raylib.LoadTextureFromImage(image);

            _fileTypeTextures[key] = textureImage;

            Raylib.UnloadImage(image);
        }
    }

    public void Render()
    {
        RenderInternal();
    }

    private void RenderInternal()
    {
        _editorControllerData.UpdateWindowPosAndSize(EditorControllerData.WindowId.DataFileExplorer);

        var windowFlags = ImGuiWindowFlags.None;
        if (ImGui.Begin("Data file explorer",
                ref _editorConfiguration.WorkspaceConfiguration.DataFileExplorerIsVisible, windowFlags))
        {
            RenderMainActions();

            ImGui.Separator();

            ImGui.BeginChild("folders/files");

            {
                windowFlags = ImGuiWindowFlags.HorizontalScrollbar;
                ImGui.BeginChild("Folders", new Vector2(ImGui.GetContentRegionAvail().X * 0.5f, 0),
                    ImGuiChildFlags.None, windowFlags);

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

            ImGui.SameLine();

            {
                var flags = ImGuiWindowFlags.HorizontalScrollbar;
                ImGui.BeginChild("Files", new Vector2(0, 0), ImGuiChildFlags.None, flags);

                RenderFiles();

                ImGui.EndChild();
            }

            ImGui.EndChild();

            RenderActiveProcess();
        }

        ImGui.End();
    }

    private void RenderFiles()
    {
        if (_selectedFolder == null)
            return;

        foreach (var fileName in _selectedFolder.Files)
        {
            var extension = Path.GetExtension(fileName);

            var texture = _fileTypeTextures.GetValueOrDefault(extension, _fileTypeTextures["?"]);

            rlImGui.ImageSize(texture, 16, 16);

            ImGui.SameLine();


            ImGui.Selectable(fileName);

            FileType? fileType = MaterialPackage.ExtensionToFileType.GetValueOrDefault(extension);

            var dragDropItemType = "";

            if (fileType == FileType.VertexShader ||
                fileType == FileType.FragmentShader)
                dragDropItemType = DragDropItemIdentifiers.ShaderFile;
            else if (fileType == FileType.Image)
                dragDropItemType = DragDropItemIdentifiers.ImageFile;

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.None) &&
                ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) &&
                fileType == FileType.Image)
            {
                var imagePath = Path.GetFullPath(Path.Combine(_selectedFolder.FullPath, fileName));
                ImageOpenRequest?.Invoke(imagePath);
            }

            if (dragDropItemType != "")
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    if (_dataFileExplorerData.DraggedFullFilePath == "")
                        Logger.Trace("Begin drag");

                    _dataFileExplorerData.DraggedRelativeFilePath =
                        Path.Combine(_selectedFolder.RelativePath, fileName);

                    _dataFileExplorerData.DraggedFullFilePath = Path.Combine(_selectedFolder.FullPath, fileName);
                    _dataFileExplorerData.DraggedFileName = fileName;

                    unsafe
                    {
                        //TODO avoid giving a fake parameter
                        var i = 1;
                        int* tesnum = &i;
                        ImGui.SetDragDropPayload(dragDropItemType, new IntPtr(tesnum), sizeof(int));
                    }

                    ImGui.Text($"{fileName}");

                    ImGui.EndDragDropSource();
                }
            }

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
        }
    }


    private void RenderFolderContent(string name,
        FolderContent folderContent)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow;

        if (_dataFileExplorerConfiguration.IsFolderOpen(folderContent.RelativePath))
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);

        if (_selectedFolder == folderContent)
            flags |= ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.SpanFullWidth;

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