using Editor.Helpers;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using NLog;
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
            var currentIndex = (variable as CodeVariableTexture).MaterialMapIndex;

            ImGui.LabelText(name, currentValue);

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
                    _editorControllerData.MaterialPackage.AddFile(draggedFileName,
                        _editorControllerData.DataFileExplorerData.DataFolder.ReadBinaryFile(
                            draggedRelativeFilePath));

                    (variable as CodeVariableTexture).Value = draggedFileName;
                    variableChanged = true;

                    _editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
                    _editorControllerData.DataFileExplorerData.DraggedFileName = "";
                }

                ImGui.EndDragDropTarget();
            }

           // ImGui.SameLine();
            var index = currentIndex == null ? -1 : (int)currentIndex.Value;
            if (ImGui.Combo("Index", ref index, EnumTools.EnumValuesToString(typeof(MaterialMapIndex), '\0')))
            {
                variableChanged = true;
                (variable as CodeVariableTexture).MaterialMapIndex = (MaterialMapIndex)index;
            }

            ImGui.EndDisabled();

            return variableChanged;
        }
    }
}