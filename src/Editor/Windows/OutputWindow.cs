using Editor.Configuration;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;
namespace Editor.Windows;

class OutputWindow(EditorConfiguration editorConfiguration,
    EditorControllerData editorControllerData)
{
    public event Action<EditorConfiguration.ModelType, string>? ModelTypeChangeRequest;
    public event Action<EditorConfiguration.BackgroundType>? BackgroundChanged;

    public void RenderOutputWindow()
    {
        var backgroundChangeIsRequested = false;
        var modelTypeChangeIsRequested = false;

        var wantedBackground = editorConfiguration.Background;
        var wantedModelFilePath = editorConfiguration.CurrentModelFilePath;
        var wantedModelType = editorConfiguration.CurrentModelType;

        editorControllerData.UpdateWindowPosAndSize(EditorControllerData.WindowId.Output);

        if (ImGui.Begin("Output", ImGuiWindowFlags.NoResize))
        {
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

            ImGui.SameLine(40);

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

            ImGui.BeginDisabled(editorConfiguration.CurrentModelType != EditorConfiguration.ModelType.Model);

            if (ImGui.BeginCombo("models", Path.GetFileName(editorConfiguration.CurrentModelFilePath)))
            {
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

                ImGui.Separator();

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



            ImGui.Separator();
            rlImGui.ImageRenderTexture(editorControllerData.ViewTexture);
        }

        ImGui.End();

        if (modelTypeChangeIsRequested)
        {
            ModelTypeChangeRequest?.Invoke(wantedModelType, wantedModelFilePath);
        }

        if(backgroundChangeIsRequested)
            BackgroundChanged?.Invoke(wantedBackground);
    }
}