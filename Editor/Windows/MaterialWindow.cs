using Editor.Helpers;
using ImGuiNET;
using Library.Helpers;
using Library.Packaging;
using NLog;
using Raylib_cs;
using System.ComponentModel;
using System.Text;

namespace Editor.Windows;


class MaterialWindow(EditorControllerData editorControllerData)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public event Action? OnSave;

    private readonly VariablesControl _variablesControl = new(editorControllerData);

    public void Render()
    {
        //ImGui.SetNextWindowSize(new Vector2(100, 80), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("MaterialMeta"))
        {
            RenderToolBar();

            RenderMeta();

            RenderShaders();

            RenderFiles();

            var material = editorControllerData.MaterialPackage.Meta;
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
        ImGui.LabelText(fileType.ToString(), file != null ? file : "");

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
        //if (ImGui.BeginChild("ToolBar", new Vector2(-1,-1)))
        //{
        //ImGui.BeginDisabled(_materialPackage.Meta.IsModified == false);

        var saveMaterial = false;

        //if (_materialPackage.Meta.IsModified)
        //    ImGui.PushStyleColor(ImGuiCol.Button, TypeConvertors.ToVector4(System.Drawing.Color.Red));

        if (ImGui.Button("Save"))
            saveMaterial = true;

        //if (_materialPackage.Meta.IsModified)
        //    ImGui.PopStyleColor(1);

        if (saveMaterial)
            OnSave?.Invoke();


        // ImGui.EndDisabled();
        //}
        //ImGui.EndChild();
    }

    private void RenderMeta()
    {
        //if (ImGui.BeginChild("Meta"))
        //{
        ImGui.SeparatorText("Meta");
        var fileName = editorControllerData.MaterialPackage.Meta.FileName;
        if (ImGui.InputText("FileName", ref fileName, 200))
        {
            editorControllerData.MaterialPackage.Meta.FileName = fileName;
            editorControllerData.MaterialPackage.Meta.SetModified();
        }

        ImGui.LabelText("FilePath", editorControllerData.MaterialPackage.Meta.FullFilePath);
        if (ImGui.InputText("Description", ref editorControllerData.MaterialPackage.Meta.Description, 200))
            editorControllerData.MaterialPackage.Meta.SetModified();
        if (ImGui.InputText("Author", ref editorControllerData.MaterialPackage.Meta.Author, 200))
            editorControllerData.MaterialPackage.Meta.SetModified();

        ImGui.BeginDisabled();
        var isModified = editorControllerData.MaterialPackage.Meta.IsModified;
        ImGui.Checkbox("is modified", ref isModified);
        ImGui.EndDisabled();
        //}
        //ImGui.EndChild();
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
                    ImGui.TextColored(TypeConvertors.ColorToVec4(System.Drawing.Color.Orange), "unused!");
                    ImGui.SameLine();
                    ImGui.PushID(file.ToString());
                    if (ImGui.Button("delete"))
                    {
                        editorControllerData.MaterialPackage.DeleteFile(file);
                    }
                    ImGui.PopID();
                }
            }

        //if (editorControllerData.DataFileExplorerData.DraggedFullFilePath != "")
        //    ImGui.Text("Drop your file here");

        //if (ImGui.BeginDragDropTarget())
        //{
        //    var payload = ImGui.AcceptDragDropPayload(DragDropItemIdentifiers.ShaderFile);

        //    bool isDropping;
        //    unsafe //TODO avoid setting unsafe to entire project
        //    {
        //        isDropping = payload.NativePtr != null;
        //    }

        //    if (isDropping)
        //    {
        //        var draggedRelativeFilePath = editorControllerData.DataFileExplorerData.DraggedFullFilePath;
        //        Logger.Trace($"dropped {draggedRelativeFilePath}");

        //        var draggedFileName = editorControllerData.DataFileExplorerData.DraggedFileName;
        //        editorControllerData._materialPackage.AddFile(draggedFileName,
        //            editorControllerData.DataFileExplorerData.DataFolder.ReadBinaryFile(draggedRelativeFilePath));

        //        editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
        //        editorControllerData.DataFileExplorerData.DraggedFileName = "";
        //    }

        //    ImGui.EndDragDropTarget();
        //}
    }


}