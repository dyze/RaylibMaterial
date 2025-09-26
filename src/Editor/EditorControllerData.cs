using Editor.Configuration;
using Editor.Windows;
using ImGuiNET;
using Library.Packaging;
using NLog;
using Raylib_cs;
using System.Numerics;

namespace Editor;

public class EditorControllerData(EditorConfiguration editorConfiguration)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public DataFileExplorerData DataFileExplorerData { get; set; } = new();

    public MaterialPackage MaterialPackage = new();

    /// <summary>
    /// null if new material
    /// </summary>
    public string? MaterialFilePath { get; set; }

    public RenderTexture2D ViewTexture;

    public readonly Dictionary<EditorConfiguration.ModelType, ToolConfig> Tools = new()
    {
        { EditorConfiguration.ModelType.Cube, new ToolConfig("cube", "cube.png") },
        { EditorConfiguration.ModelType.Plane, new ToolConfig("plane", "plane.png") },
        { EditorConfiguration.ModelType.Sphere, new ToolConfig("sphere", "sphere.png") },
        { EditorConfiguration.ModelType.Model, new ToolConfig("model", "model.png") }
    };

    public List<string> BuiltInModels = [];


    public readonly Dictionary<EditorConfiguration.BackgroundType, BackgroundConfig> Backgrounds = new()
    {
        { EditorConfiguration.BackgroundType.Cloud, new BackgroundConfig("clouds", "clouds.jpg") },
        { EditorConfiguration.BackgroundType.WildPark, new BackgroundConfig("wild park", "wildpark.png") },
        { EditorConfiguration.BackgroundType.Space, new BackgroundConfig("space", "space.jpg") },
    };

    public bool WorkspaceLayoutResetRequested { get; set; }

    public ImGuiCond WindowPosSizeCondition
    {
        get
        {
            if (WorkspaceLayoutResetRequested)
                return ImGuiCond.Always;
            return ImGuiCond.FirstUseEver;
        }
    }

    public void ResetWorkspaceLayout()
    {
        Logger.Trace("ResetWorkspaceLayout...");
        WorkspaceLayoutResetRequested = true;
    }

    public enum WindowId
    {
        Material,
        Code,
        Message,
        DataFileExplorer,
        Output
    }

    public void UpdateWindowPosAndSize(WindowId windowId)
    {
        Vector2 finalPosition;
        Vector2 finalSize;

        var outputSize = editorConfiguration.OutputSize;
        var toolbarSize = new Vector2(0, 60);
        var wholeOutputSize = outputSize + toolbarSize + new Vector2(0, ImGui.GetFrameHeightWithSpacing());

        var menuSize = new Vector2(0, 20);

        var screenSizeMinusMenu = editorConfiguration.ScreenSize - menuSize;

        const int materialWindowWidth = 200;

        switch (windowId)
        {
            case WindowId.Material:
                finalSize = new Vector2(materialWindowWidth, 
                    screenSizeMinusMenu.Y * 0.6f);
                finalSize -= new Vector2(0, ImGui.GetFrameHeightWithSpacing());
                finalPosition = menuSize;
                break;
            case WindowId.Code:
                finalSize = new Vector2(editorConfiguration.ScreenSize.X - editorConfiguration.OutputSize.X - 200,
                    screenSizeMinusMenu.Y * 0.6f);
                finalSize -= new Vector2(0, ImGui.GetFrameHeightWithSpacing()); 

                finalPosition = new Vector2(materialWindowWidth, menuSize.Y);
                break;
            case WindowId.Message:
                finalSize = new Vector2(screenSizeMinusMenu.X - materialWindowWidth,
                    screenSizeMinusMenu.Y * 0.4f);
                finalSize -= new Vector2(0, ImGui.GetFrameHeightWithSpacing());
                finalPosition = new Vector2(editorConfiguration.ScreenSize.X - finalSize.X,
                    editorConfiguration.ScreenSize.Y - finalSize.Y);
                break;
            case WindowId.DataFileExplorer:
                finalSize = new Vector2(materialWindowWidth, 
                    screenSizeMinusMenu.Y * 0.4f);
                finalSize -= new Vector2(0, ImGui.GetFrameHeightWithSpacing());

                finalPosition = new Vector2(0, screenSizeMinusMenu.Y - finalSize.Y);
                break;
            case WindowId.Output:
                finalSize = wholeOutputSize;
                finalPosition = new Vector2(editorConfiguration.ScreenSize.X - outputSize.X,
                    menuSize.Y);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(windowId), windowId, null);
        }

        ImGui.SetNextWindowSize(finalSize,
            WindowPosSizeCondition);

        ImGui.SetNextWindowPos(finalPosition, 
            WindowPosSizeCondition);


    }
}