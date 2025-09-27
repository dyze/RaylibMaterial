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
        { EditorConfiguration.BackgroundType.Cloud, new BackgroundConfig("clouds", "Daylight Box UV.png") },
        //{ EditorConfiguration.BackgroundType.WildPark, new BackgroundConfig("wild park", "wildpark.png") },
        { EditorConfiguration.BackgroundType.NightSky, new BackgroundConfig("night sky", "night-sky.png") },
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

    public Vector2 UpdateWindowPosAndSize(WindowId windowId)
    {
        Vector2 finalPosition;
        Vector2 finalSize;

        var manWindowSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());


        var marging = new Vector2(20f, 20f);
        var spacing = new Vector2(10f, 10f);

        var menuSize = new Vector2(0, 20);

        var mainWindowSizeMinusMenu = new Vector2(manWindowSize.X,
            manWindowSize.Y) - menuSize;

        var materialWindowWidth = mainWindowSizeMinusMenu.X * 0.2f - marging.X - spacing.X;
        var outputWidth = mainWindowSizeMinusMenu.X * 0.5f - marging.X - spacing.X;
        var codeWidth = mainWindowSizeMinusMenu.X * 0.3f - spacing.X - spacing.X;

        var heightRatioTop = 0.7f;
        var heightRatioBottom = 1f - heightRatioTop;

        var topHeight = mainWindowSizeMinusMenu.Y * heightRatioTop - marging.Y - spacing.Y;
        var bottomHeight = mainWindowSizeMinusMenu.Y * heightRatioBottom - marging.Y - spacing.Y;

        switch (windowId)
        {
            case WindowId.Material:
                finalSize = new Vector2(materialWindowWidth,
                    topHeight);

                finalPosition = new Vector2(spacing.X,
                    menuSize.Y + marging.Y);
                break;
            case WindowId.Code:
                finalSize = new Vector2(codeWidth,
                    topHeight);

                finalPosition = new Vector2(materialWindowWidth + marging.X + spacing.X,
                    menuSize.Y + marging.Y);
                break;
            case WindowId.Message:
                finalSize = new Vector2(mainWindowSizeMinusMenu.X - materialWindowWidth,
                    bottomHeight);

                finalPosition = new Vector2(materialWindowWidth + marging.X + spacing.X,
                    mainWindowSizeMinusMenu.Y - finalSize.Y);
                break;
            case WindowId.DataFileExplorer:
                finalSize = new Vector2(materialWindowWidth,
                    bottomHeight);

                finalPosition = new Vector2(spacing.X,
                    mainWindowSizeMinusMenu.Y - finalSize.Y);
                break;
            case WindowId.Output:
                finalSize = new Vector2(outputWidth,
                    topHeight);

                finalPosition = new Vector2(marging.X + materialWindowWidth + spacing.X + codeWidth + spacing.X,
                    menuSize.Y + marging.Y);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(windowId), windowId, null);
        }

        ImGui.SetNextWindowSize(finalSize,
            WindowPosSizeCondition);

        ImGui.SetNextWindowPos(finalPosition,
            WindowPosSizeCondition);

        return finalSize;
    }
}