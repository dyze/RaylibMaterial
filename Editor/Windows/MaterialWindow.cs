using ImGuiNET;
using NLog;

namespace Editor.Windows;


class MaterialWindow(EditorControllerData editorControllerData)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Render()
    {
        //ImGui.SetNextWindowSize(new Vector2(100, 80), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("MaterialMeta"))
        {
            RenderMaterialToolBar();

            //ImGui.Separator();

            RenderMeta();

            //ImGui.Separator();

            RenderMaterialFiles();

            if (VariablesControl.Render(editorControllerData._materialPackage.Meta.Variables))
            {
                editorControllerData._materialPackage.Meta.SetModified();
                //ApplyVariables();
            }
        }

        ImGui.End();
    }

    private void RenderMaterialToolBar()
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

        //if (saveMaterial)
        //    SaveMaterial();


        // ImGui.EndDisabled();
        //}
        //ImGui.EndChild();
    }

    private void RenderMeta()
    {
        //if (ImGui.BeginChild("Meta"))
        //{
        ImGui.SeparatorText("Meta");
        var fileName = editorControllerData._materialPackage.Meta.FileName;
        if (ImGui.InputText("FileName", ref fileName, 200))
        {
            editorControllerData._materialPackage.Meta.FileName = fileName;
            editorControllerData._materialPackage.Meta.SetModified();
        }

        ImGui.LabelText("FilePath", editorControllerData._materialPackage.Meta.FullFilePath);
        if (ImGui.InputText("Description", ref editorControllerData._materialPackage.Meta.Description, 200))
            editorControllerData._materialPackage.Meta.SetModified();
        if (ImGui.InputText("Author", ref editorControllerData._materialPackage.Meta.Author, 200))
            editorControllerData._materialPackage.Meta.SetModified();

        ImGui.BeginDisabled();
        var isModified = editorControllerData._materialPackage.Meta.IsModified;
        ImGui.Checkbox("is modified", ref isModified);
        ImGui.EndDisabled();
        //}
        //ImGui.EndChild();
    }


    private void RenderMaterialFiles()
    {
        //if (ImGui.BeginChild("Files"))
        //{
        ImGui.SeparatorText("Files");
        if(editorControllerData._materialPackage.Files.Count == 0)
            ImGui.TextDisabled("Empty");
        else
            foreach (var file in editorControllerData._materialPackage.Files)
            {
                ImGui.Text(file.Key.FileName);
            }
        //}
        //ImGui.EndChild();

        if(ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            if(editorControllerData.DataFileExplorerData.DraggedFile != "")
                ImGui.Text("Drop your file here");

        if (ImGui.BeginDragDropTarget())
        {
            var acceptPayload = ImGui.AcceptDragDropPayload(DragDropItemIdentifiers.File);

            bool isDropping;
            unsafe //TODO avoid setting unsafe to entire project
            {
                isDropping = acceptPayload.NativePtr != null;
            }

            if (isDropping)
            {
                var droppedFile = editorControllerData.DataFileExplorerData.DraggedFile;
                Logger.Trace($"dropped {droppedFile}");
                //_engine.Entities.AddEntityFromPrefab(droppedFile, entity, Vector2.Zero, 0);
                editorControllerData.DataFileExplorerData.DraggedFile = "";
            }

            ImGui.EndDragDropTarget();
        }
    }


}