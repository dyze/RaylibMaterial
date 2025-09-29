using Editor.Configuration;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;
using NLog;
using Raylib_cs;

namespace Editor.Windows;

class OutputWindow(EditorConfiguration editorConfiguration,
    EditorControllerData editorControllerData)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public event Action<EditorConfiguration.ModelType, string>? ModelTypeChangeRequest;
    public event Action<string>? BackgroundChanged;
    public event Action<EditorConfiguration.LightingPreset>? LightingPresetChangeRequest;
    public event Action? ResetCameraIsRequest;

    private Vector2? _previousSize;

    public void RenderOutputWindow()
    {
        var backgroundChangeIsRequested = false;
        var modelTypeChangeIsRequested = false;
        var lightingPresetChangeIsRequested = false;
        var resetCameraIsRequested = false;

        var wantedBackground = editorConfiguration.Background;
        var wantedModelFilePath = editorConfiguration.CurrentModelFilePath;
        var wantedModelType = editorConfiguration.CurrentModelType;

        var wantedLightingPreset = editorConfiguration.CurrentLightingPreset;

        editorControllerData.UpdateWindowPosAndSize(EditorControllerData.WindowId.Output);


        if (ImGui.Begin("Output", ImGuiWindowFlags.None))
        {
            var newSize = ImGui.GetWindowSize();
            if (newSize != _previousSize)
            {
                Logger.Trace($"{nameof(OutputWindow)}: window size changed {_previousSize}->{newSize}");

                const int toolBarHeight = 110;

                editorControllerData.ViewTexture = Raylib.LoadRenderTexture((int)newSize.X, (int)newSize.Y - toolBarHeight);
            }

            _previousSize = newSize;

            foreach (var (key, tool) in editorControllerData.Tools)
            {
                ImGui.SameLine();
                if (rlImGui.ImageButtonSize(tool.Name,
                        tool.Texture,
                        new Vector2(32, 32)))
                {
                    wantedModelType = key;
                    modelTypeChangeIsRequested = true;
                    break;
                }
            }

            ImGui.SameLine();

            foreach (var (key, background) in editorControllerData.Backgrounds)
            {
                ImGui.SameLine();
                if (rlImGui.ImageButtonSize(background.Name,
                        background.Texture,
                        new Vector2(32, 32)))
                {
                    backgroundChangeIsRequested = true;
                    wantedBackground = key;
                    break;
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Reset camera"))
            {
                resetCameraIsRequested = true;
            }


            ImGui.BeginDisabled(editorConfiguration.CurrentModelType != EditorConfiguration.ModelType.Model);

            if (ImGui.BeginCombo("models", Path.GetFileName(editorConfiguration.CurrentModelFilePath)))
            {
                ImGui.SeparatorText("Build in");

                // Built in models
                ImGui.PushID("BuildIn");
                foreach (var model in editorControllerData.BuiltInModels)
                {
                    var selected = model == editorConfiguration.CurrentModelFilePath;
                    if (ImGui.Selectable(Path.GetFileName(model),
                            selected))
                    {
                        wantedModelFilePath = model;
                        modelTypeChangeIsRequested = true;
                        break;
                    }
                }
                ImGui.PopID();

                ImGui.SeparatorText("Custom");

                // Custom models
                ImGui.PushID("Custom");
                foreach (var model in editorConfiguration.CustomModels)
                {
                    var selected = model == editorConfiguration.CurrentModelFilePath;
                    if (ImGui.Selectable(Path.GetFileName(model),
                            selected))
                    {
                        wantedModelFilePath = model;
                        modelTypeChangeIsRequested = true;
                        break;
                    }
                }
                ImGui.PopID();

                ImGui.EndCombo();
            }

            ImGui.EndDisabled();


            if (ImGui.BeginCombo("lighting", editorConfiguration.CurrentLightingPreset.ToString()))
            {

                // Built in models
                ImGui.PushID("lighting");
                foreach (var preset in Enum.GetValues<EditorConfiguration.LightingPreset>())
                {
                    var selected = preset == editorConfiguration.CurrentLightingPreset;
                    if (ImGui.Selectable(preset.ToString(),
                            selected))
                    {
                        wantedLightingPreset = preset;
                        lightingPresetChangeIsRequested = true;
                        break;
                    }
                }
                ImGui.PopID();

                ImGui.SeparatorText("Custom");

                // Custom models
                ImGui.PushID("Custom");
                foreach (var model in editorConfiguration.CustomModels)
                {
                    var selected = model == editorConfiguration.CurrentModelFilePath;
                    if (ImGui.Selectable(Path.GetFileName(model),
                            selected))
                    {
                        wantedModelFilePath = model;
                        modelTypeChangeIsRequested = true;
                        break;
                    }
                }
                ImGui.PopID();

                ImGui.EndCombo();
            }



            ImGui.Separator();
            rlImGui.ImageRenderTexture(editorControllerData.ViewTexture);
        }

        ImGui.End();

        if (modelTypeChangeIsRequested)
            ModelTypeChangeRequest?.Invoke(wantedModelType, wantedModelFilePath);

        if(backgroundChangeIsRequested)
            BackgroundChanged?.Invoke(wantedBackground);

        if(lightingPresetChangeIsRequested)
            LightingPresetChangeRequest?.Invoke(wantedLightingPreset);

        if(resetCameraIsRequested)
            ResetCameraIsRequest?.Invoke();
    }
}