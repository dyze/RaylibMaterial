using Editor.Helpers;
using ImGuiNET;
using Library.Helpers;
using Library.Packaging;
using NLog;
using System.Text;

namespace Editor.Windows;


class MaterialWindow(EditorControllerData editorControllerData)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public event Action? OnSave;

    private readonly VariablesControl _variablesControl = new(editorControllerData);

    public void Render()
    {
        if (ImGui.Begin("Material"))
        {
            RenderToolBar();
            RenderMeta();
            RenderShaders();
            RenderFiles();

            var material = editorControllerData.MaterialPackage;
            if (_variablesControl.Render(material.Variables))
            {
                material.SetModified();
                material.TriggerVariablesChanged();
            }
        }

        ImGui.End();
    }



    private void RenderShaders()
    {
        RenderShaderField(FileType.VertexShader);

        RenderShaderField(FileType.FragmentShader);
    }

    private void RenderShaderField(FileType fileType)
    {
        var material = editorControllerData.MaterialPackage;
        var file = material.GetShaderName(fileType);
        ImGui.LabelText(fileType.ToString(), file ?? "");

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
    }

    private void RenderToolBar()
    {
        var saveMaterial = false;

        var material = editorControllerData.MaterialPackage;

        if (material.IsModified)
            ImGui.PushStyleColor(ImGuiCol.Button, 
                TypeConvertors.ColorToVector4(System.Drawing.Color.Red));

        if (ImGui.Button("Save"))
            saveMaterial = true;

        if (material.IsModified)
            ImGui.PopStyleColor(1);

        if (saveMaterial)
            OnSave?.Invoke();
    }

    private void RenderMeta()
    {
        ImGui.SeparatorText("Properties");

        if (ImGui.InputText("Description", ref editorControllerData.MaterialPackage.Meta.Description, 200))
            editorControllerData.MaterialPackage.SetModified();
        if (ImGui.InputText("Author", ref editorControllerData.MaterialPackage.Meta.Author, 200))
            editorControllerData.MaterialPackage.SetModified();
    }


    private void RenderFiles()
    {
        ImGui.SeparatorText("Files"); 
        ImGuiHelpers.HelpMarker("Files that will be part of final package");
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
                    ImGui.TextColored(TypeConvertors.ColorToVector4(System.Drawing.Color.Orange), "unused!");
                    ImGui.SameLine();
                    ImGui.PushID(file.ToString());
                    if (ImGui.Button("delete"))
                    {
                        editorControllerData.MaterialPackage.DeleteFile(file.Key);
                    }
                    ImGui.PopID();
                }
            }

    }


}