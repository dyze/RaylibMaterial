using Editor.Configuration;
using Editor.EditorControllerNS;
using Editor.Helpers;
using Editor.Processes;
using ImGuiNET;
using Library.Packaging;
using NLog;
using Raylib_cs;
using rlImGui_cs;
using System.Diagnostics;
using System.Numerics;

namespace Editor.Ui.Windows;

public class DataFileExplorer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly EditorConfiguration _editorConfiguration;
    private readonly DataFileExplorerConfiguration _dataFileExplorerConfiguration;
    private readonly DataFileExplorerData _dataFileExplorerData;

    public DirectoryInfo? DirectoryPath;

    public List<FileInfo> CurrentFiles = [];
    public List<DirectoryInfo> CurrentDirectories = [];
    public int CurrentDirectoryIndex = -1;

    public DirectoryInfo? CurrentDirectory
    {
        get
        {
            if (CurrentDirectoryIndex < 0)
                return null;
            return CurrentDirectories[CurrentDirectoryIndex];
        }
    }

    private string _currentExtension = "*.*";


    /// <summary>
    /// true to trig a refresh of information during next tick
    /// </summary>
    private bool _refreshInfo;

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
        { "refresh", renderer => renderer.OnRefresh() },
        { "pin path", renderer => renderer.AddToFavourite() },
    };

    private readonly Dictionary<string, Action<DataFileExplorer, string>> _folderActions = new()
    {
        { "explore folder", (_, path) => Process.Start("explorer.exe", path) },
    };

    private readonly EditorControllerData _editorControllerData;

    public Action<string>? ImageOpenRequest;

    public DataFileExplorer(EditorConfiguration editorConfiguration,
        EditorControllerData editorControllerData,
        DataFileExplorerData dataFileExplorerData)
    {
        _editorConfiguration = editorConfiguration;
        _dataFileExplorerConfiguration = editorConfiguration.DataFileExplorerConfiguration;
        _editorControllerData = editorControllerData;
        _dataFileExplorerData = dataFileExplorerData; 
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
            if (DirectoryPath == null ||
               _refreshInfo)
                RetrieveInfo();

            RenderPaths();

            ImGui.Separator();

            RenderMainActions();

            ImGui.Separator();

            ImGui.BeginChild("folders/files");

            {
                windowFlags = ImGuiWindowFlags.HorizontalScrollbar;
                ImGui.BeginChild("Folders", new Vector2(ImGui.GetContentRegionAvail().X * 0.5f, 0),
                    ImGuiChildFlags.None, windowFlags);

                RenderDirectories();

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

    private void RenderDirectories()
    {
        var contentRegionWidth = ImGui.GetContentRegionAvail().X;

        for (var index = 0; index < CurrentDirectories.Count; ++index)
        {
            if (index == 0)
            {
                if (ImGui.Selectable(".",
                        CurrentDirectoryIndex == index,
                        ImGuiSelectableFlags.AllowDoubleClick,
                        new Vector2(contentRegionWidth, 0)))
                {
                    CurrentDirectoryIndex = index;
                    _refreshInfo = true;
                }
                continue;
            }

            if (index == 1)
            {
                if (ImGui.Selectable("..",
                        CurrentDirectoryIndex == index,
                        ImGuiSelectableFlags.AllowDoubleClick,
                        new Vector2(contentRegionWidth, 0)))
                {
                    CurrentDirectoryIndex = index;
                    _refreshInfo = true;

                    if(DirectoryPath.Parent != null)
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            _dataFileExplorerConfiguration.ResourcesPath = DirectoryPath.Parent.FullName;
                            CurrentDirectoryIndex = -1;
                            _refreshInfo = true;
                        }
                    }
                }
                continue;
            }


            var directoryEntry = CurrentDirectories[index];
            var directoryName = directoryEntry.Name;

            contentRegionWidth = ImGui.GetContentRegionAvail().X;

            if (ImGui.Selectable(directoryName, CurrentDirectoryIndex == index,
                    ImGuiSelectableFlags.AllowDoubleClick, new Vector2(contentRegionWidth, 0)))
            {
                CurrentDirectoryIndex = index;
                _refreshInfo = true;

                if (ImGui.IsMouseDoubleClicked(0))
                {
                    _dataFileExplorerConfiguration.ResourcesPath = directoryEntry.FullName;
                    CurrentDirectoryIndex = -1;
                    _refreshInfo = true;
                }
            }

            if (ImGui.BeginPopupContextItem())
            {
                RenderDirectoryActions(directoryEntry.FullName);
                ImGui.EndPopup();
            }
        }
    }

    private void RenderPaths()
    {
        var directories = _dataFileExplorerConfiguration.FavouriteDirectories.ToArray();

        if (ImGui.BeginCombo("Paths", _dataFileExplorerConfiguration.ResourcesPath))
        {
            // Always show current path first
            ImGui.Text(_dataFileExplorerConfiguration.ResourcesPath);

            ImGui.SeparatorText("Favourites");

            for (var i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];

                ImGui.PushID(i);

                ImGui.SetNextItemAllowOverlap();

                if (ImGui.Selectable(directory, false))
                {
                    Logger.Trace($"FavouriteDirectory changed {directory}");

                    _dataFileExplorerConfiguration.ResourcesPath = directory;

                    CurrentDirectoryIndex = -1;
                    _refreshInfo = true;
                }

                if (i >= 1)        // 0=editor resource path, can't be unpin
                {
                    ImGui.SameLine();
                    if (ImGui.Button("unpin"))
                        RemoveFavourite(i);
                }

                ImGui.PopID();
            }


            ImGui.EndCombo();
        }
    }

    private void RetrieveInfo()
    {
        _refreshInfo = false;

        CurrentFiles.Clear();
        CurrentDirectories.Clear();

        DirectoryPath = new DirectoryInfo(_dataFileExplorerConfiguration.ResourcesPath);
        CurrentDirectories = DirectoryPath.GetDirectories().ToList();

        CurrentDirectories.Insert(0, null); // For "."
        CurrentDirectories.Insert(1, null); // For ".."

        try
        {
            var currentDirectory = CurrentDirectory;
            if (currentDirectory != null)
                CurrentFiles = new DirectoryInfo(CurrentDirectory.FullName).GetFiles(CurrentExtension).ToList();
            else
                CurrentFiles = DirectoryPath.GetFiles(CurrentExtension).ToList();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }


    public string CurrentExtension
    {
        get => _currentExtension;

        set
        {
            _currentExtension = value;
            RetrieveInfo();
        }
    }

    private void RenderFiles()
    {
        foreach (var file in CurrentFiles)
        {
            var extension = Path.GetExtension(file.FullName);

            var texture = _fileTypeTextures.GetValueOrDefault(extension, _fileTypeTextures["?"]);

            rlImGui.ImageSize(texture, 16, 16);

            ImGui.SameLine();

            ImGui.Selectable(file.Name);

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
                var imagePath = file.FullName;
                ImageOpenRequest?.Invoke(imagePath);
            }

            if (dragDropItemType != "")
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    if (_dataFileExplorerData.DraggedFullFilePath == "")
                        Logger.Trace("Begin drag");

                    _dataFileExplorerData.DraggedFullFilePath = file.FullName;

                    unsafe
                    {
                        //TODO avoid giving a fake parameter
                        var i = 1;
                        int* tesnum = &i;
                        ImGui.SetDragDropPayload(dragDropItemType, new nint(tesnum), sizeof(int));
                    }

                    ImGui.Text($"{file}");

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

    private void RenderMainActions()
    {
        var first = true;
        foreach (var (key, action) in _mainActions)
        {
            if (first == false)
                ImGui.SameLine();

            if (ImGui.Button(key))
                action(this);

            first = false;
        }
    }

    private void RenderDirectoryActions(string path)
    {
        foreach (var (key, action) in _folderActions)
        {
            if (ImGui.Selectable(key))
                action(this, path);
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
        _refreshInfo = true;
    }

    private void AddToFavourite()
    {
        _dataFileExplorerConfiguration.AddToFavourite(DirectoryPath.FullName);
    }

    private void RemoveFavourite(int index)
    {
        _dataFileExplorerConfiguration.RemoveFavourite(index);
    }
}