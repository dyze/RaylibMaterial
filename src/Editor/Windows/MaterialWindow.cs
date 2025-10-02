using Editor.Configuration;
using Editor.Helpers;
using ImGuiNET;
using Library.CodeVariable;
using Library.Packaging;
using NLog;
using System.Text;
using System.Xml.Linq;

namespace Editor.Windows;

class MaterialWindow(
    EditorConfiguration editorConfiguration,
    EditorControllerData editorControllerData)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public event Action? OnSave;

    private readonly VariablesControls _variablesControls = new(editorControllerData);

    public void Render()
    {
        editorControllerData.UpdateWindowPosAndSize(EditorControllerData.WindowId.Material);

        if (ImGui.Begin("Material"))
        {
            RenderToolBar();
            RenderProperties();
            RenderFiles();

            var material = editorControllerData.MaterialPackage;
            if (_variablesControls.Render(material.Variables))
            {
                material.SetModified();
                material.TriggerVariablesChanged();
            }
        }

        ImGui.End();
    }

    private void RenderShaderField(FileType fileType)
    {
        ImGui.PushID(fileType.ToString());

        var material = editorControllerData.MaterialPackage;
        var file = material.GetShaderName(fileType);

        if (file != "")
        {
            if (ImGui.Button("x"))
            {
                material.SetShaderName(fileType, "");
            }

            ImGui.SameLine();
        }

        {
            var files = editorControllerData.MaterialPackage.GetFilesMatchingType(fileType);
            var currentIndex = files.FindIndex(i => i == file);

            if (ImGui.Combo(fileType.ToString(), ref currentIndex, files.ToArray(), files.Count))
            {
                material.SetShaderName(fileType, files[currentIndex]);
            }
        }

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(DragDropItemIdentifiers.ShaderFile);

            bool isDropping;
            unsafe //TODO avoid setting unsafe to entire project
            {
                isDropping = payload.NativePtr != null;
            }

            if (isDropping)
            {
                var draggedFullFilePath = editorControllerData.DataFileExplorerData.DraggedFullFilePath;
                Logger.Trace($"dropped {draggedFullFilePath}");

                var extension = Path.GetExtension(draggedFullFilePath);
                if (MaterialPackage.ExtensionToFileType[extension] == fileType)
                {
                    var fileContent = File.ReadAllText(draggedFullFilePath);

                    string draggedFileName = editorControllerData.DataFileExplorerData.DraggedFileName;
                    editorControllerData.MaterialPackage.AddFile(draggedFileName,
                        Encoding.ASCII.GetBytes(fileContent));

                    editorControllerData.MaterialPackage.SetShaderName(fileType, draggedFileName);

                    editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
                    draggedFileName = "";
                }
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.PopID();
    }

    private void RenderToolBar()
    {
        var saveMaterial = false;

        var material = editorControllerData.MaterialPackage;

        if (material.IsModified)
            ImGui.PushStyleColor(ImGuiCol.Button,
                TypeConverters.ColorToVector4(System.Drawing.Color.Red));

        if (ImGui.Button("Save"))
            saveMaterial = true;

        if (material.IsModified)
            ImGui.PopStyleColor(1);

        if (saveMaterial)
            OnSave?.Invoke();
    }

    private void RenderProperties()
    {
        if(ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SameLine();
            HelpMarker.Run("Description of the material");

            if (ImGui.InputText("Description", ref editorControllerData.MaterialPackage.Meta.Description, 200))
                editorControllerData.MaterialPackage.SetModified();
            if (ImGui.InputText("Author", ref editorControllerData.MaterialPackage.Meta.Author, 200))
                editorControllerData.MaterialPackage.SetModified();

            RenderShaderField(FileType.VertexShader);

            RenderShaderField(FileType.FragmentShader);
        }
    }


    private void RenderFiles()
    {
        if (ImGui.CollapsingHeader("Files", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SameLine();
            HelpMarker.Run("Files that will be part of final package");

            if (editorControllerData.MaterialPackage.FilesCount == 0)
                ImGui.TextDisabled("Empty");
            else
                foreach (var file in editorControllerData.MaterialPackage.Files)
                {
                    var fileReferences = editorControllerData.MaterialPackage.FileReferences[file.Key];
                    ImGui.Text(file.Key.FileName);

                    if (fileReferences == 0)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(TypeConverters.ColorToVector4(System.Drawing.Color.Orange), "unused!");
                        ImGui.SameLine();
                        ImGui.PushID("delete" + file.ToString());
                        if (ImGui.Button("delete"))
                        {
                            editorControllerData.MaterialPackage.DeleteFile(file.Key);
                        }

                        ImGui.PopID();
                    }

                    if (file.Key.FileType == FileType.FragmentShader ||
                        file.Key.FileType == FileType.VertexShader)
                    {
                        ImGui.SameLine();
                        ImGui.PushID("activate" + file.ToString());
                        if (ImGui.Button("activate"))
                        {
                            editorControllerData.MaterialPackage.ActivateShader(file.Key);
                        }

                        ImGui.PopID();
                    }
                }
        }
    }
}