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


    public Dictionary<string, BackgroundConfig> Backgrounds = new();

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


        var margin = new Vector2(10f, 10f);
        var spacing = new Vector2(10f, 10f);

        var menuSize = new Vector2(0, 20);

        var availableArea = new Vector2(manWindowSize.X,
            manWindowSize.Y) - menuSize;

        var materialWindowWidth = availableArea.X * 0.2f - margin.X - spacing.X/2;
        var outputWidth = availableArea.X * 0.4f - spacing.X / 2 - margin.X;
        var codeWidth = availableArea.X * 0.4f - spacing.X / 2 - spacing.X / 2;

        var heightRatioTop = 0.8f;
        var heightRatioBottom = 1f - heightRatioTop;

        var topHeight = availableArea.Y * heightRatioTop - margin.Y - spacing.Y/2;
        var bottomHeight = availableArea.Y * heightRatioBottom - margin.Y - spacing.Y/2;

        switch (windowId)
        {
            case WindowId.Material:
                finalSize = new Vector2(materialWindowWidth,
                    topHeight);

                finalPosition = new Vector2(margin.X,
                    menuSize.Y + margin.Y);
                break;
            case WindowId.Code:
                finalSize = new Vector2(codeWidth,
                    topHeight);

                finalPosition = new Vector2(margin.X + materialWindowWidth + spacing.X,
                    menuSize.Y + margin.Y);
                break;
            case WindowId.Message:
                finalSize = new Vector2(availableArea.X - materialWindowWidth - margin.X*2 - spacing.X,
                    bottomHeight);

                finalPosition = new Vector2(materialWindowWidth + margin.X + spacing.X,
                    menuSize.Y + margin.Y + topHeight + spacing.Y);
                break;
            case WindowId.DataFileExplorer:
                finalSize = new Vector2(materialWindowWidth,
                    bottomHeight);

                finalPosition = new Vector2(margin.X,
                    menuSize.Y + margin.Y + topHeight + spacing.Y);
                break;
            case WindowId.Output:
                finalSize = new Vector2(outputWidth,
                    topHeight);

                finalPosition = new Vector2(margin.X + materialWindowWidth + spacing.X + codeWidth + spacing.X,
                    menuSize.Y + margin.Y);
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