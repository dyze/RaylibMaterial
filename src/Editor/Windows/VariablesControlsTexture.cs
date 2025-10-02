using Editor.Helpers;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using Library.Packaging;
using Raylib_cs;

namespace Editor.Windows
{
    partial class VariablesControls
    {
        private bool HandleTexture(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = (variable as CodeVariableTexture).Value;
            {
                if ((variable as CodeVariableTexture).Value != "")
                {
                    if (ImGui.Button("x"))
                    {
                        (variable as CodeVariableTexture).Value = "";
                        (variable as CodeVariableTexture).MaterialMapIndex = null;
                        variableChanged = true;
                    }

                    ImGui.SameLine();
                }
            }

            {
                var files = _editorControllerData.MaterialPackage.GetFilesMatchingType(FileType.Image);
                var currentIndex = files.FindIndex(i => i == currentValue);
                
                if (ImGui.Combo(name, ref currentIndex, files.ToArray(), files.Count))
                {
                    (variable as CodeVariableTexture).Value = files[currentIndex];
                    variableChanged = true;
                }
            }


            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(DragDropItemIdentifiers.ImageFile);

                bool isDropping;
                unsafe //TODO avoid setting unsafe to entire project
                {
                    isDropping = payload.NativePtr != null;
                }

                if (isDropping)
                {
                    var draggedRelativeFilePath = _editorControllerData.DataFileExplorerData
                        .DraggedRelativeFilePath;
                    Logger.Trace($"dropped {draggedRelativeFilePath}");

                    var draggedFileName = _editorControllerData.DataFileExplorerData.DraggedFileName;
                    var readBinaryFile =
                        _editorControllerData.DataFileExplorerData.DataFolder.ReadBinaryFile(draggedRelativeFilePath);
                    _editorControllerData.MaterialPackage.AddFile(draggedFileName,
                        readBinaryFile);

                    (variable as CodeVariableTexture).Value = draggedFileName;
                    variableChanged = true;

                    _editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
                    _editorControllerData.DataFileExplorerData.DraggedFileName = "";
                }

                ImGui.EndDragDropTarget();
            }


            {
                var enumNames = EnumTools.EnumNamesToString(typeof(MaterialMapIndex), '\0');
                var enumValues = Enum.GetValues<MaterialMapIndex>().ToList();

                var index = -1;
                var materialMapIndex = (variable as CodeVariableTexture).MaterialMapIndex;
                if (materialMapIndex != null)
                {
                    index = enumValues.FindIndex(0, v => v == materialMapIndex);
                }

                if (ImGui.Combo("Index", ref index, enumNames))
                {
                    variableChanged = true;
                    (variable as CodeVariableTexture).MaterialMapIndex = enumValues[index];
                }
            }

            ImGui.EndDisabled();

            return variableChanged;
        }
    }
}