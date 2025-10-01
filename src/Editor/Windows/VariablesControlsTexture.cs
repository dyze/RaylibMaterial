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


            var enumNames = EnumTools.EnumNamesToString(typeof(MaterialMapIndex), '\0');
            var enumValues = Enum.GetValues<MaterialMapIndex>().ToList();

            var index = -1;
            if (currentIndex != null)
            {
                index = enumValues.FindIndex(0, v => v == currentIndex);
            }

            if (ImGui.Combo("Index", ref index, enumNames))
            {
                variableChanged = true;
                (variable as CodeVariableTexture).MaterialMapIndex = enumValues[index];
            }

            ImGui.EndDisabled();

            return variableChanged;
        }
    }
}