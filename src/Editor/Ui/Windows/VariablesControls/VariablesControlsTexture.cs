using Editor.Helpers;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using Library.Packaging;
using Raylib_cs;

namespace Editor.Ui.Windows.VariablesControls
{
    partial class VariablesControls
    {
        public Action<string, byte[]>? ImageOpenRequest;

        private bool HandleTexture(CodeVariableTexture variable, string name)
        {
            var variableChanged = false;

            var currentValue = variable.Value;


            {
                var files = _editorControllerData.MaterialPackage.GetFilesMatchingType(FileType.Image);
                var currentIndex = files.FindIndex(i => i == currentValue);
                
                if (ImGui.Combo("Image", ref currentIndex, files.ToArray(), files.Count))
                {
                    variable.Value = files[currentIndex];
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
                    //var draggedRelativeFilePath = _editorControllerData.DataFileExplorerData
                    //    .DraggedRelativeFilePath;
                    //Logger.Trace($"dropped {draggedRelativeFilePath}");

                    var draggedFileName = Path.GetFileName(_editorControllerData.DataFileExplorerData.DraggedFullFilePath);
                    var readBinaryFile = File.ReadAllBytes(_editorControllerData.DataFileExplorerData.DraggedFullFilePath);

                    _editorControllerData.MaterialPackage.AddFile(draggedFileName,
                        readBinaryFile);

                    variable.Value = draggedFileName;
                    variableChanged = true;

                    _editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
                    //_editorControllerData.DataFileExplorerData.DraggedFileName = "";
                }

                ImGui.EndDragDropTarget();
            }


            {
                var enumNames = EnumTools.EnumNamesToString(typeof(MaterialMapIndex), '\0');
                var enumValues = Enum.GetValues<MaterialMapIndex>().ToList();

                var index = -1;
                var materialMapIndex = variable.MaterialMapIndex;
                if (materialMapIndex != null)
                {
                    index = enumValues.FindIndex(0, v => v == materialMapIndex);
                }

                if (ImGui.Combo("MaterialMapIndex", ref index, enumNames))
                {
                    variableChanged = true;
                    variable.MaterialMapIndex = enumValues[index];
                }

                {
                    if (variable.Value != "")
                    {
                        if (ImGui.Button("unassign"))
                        {
                            variable.Value = "";
                            variable.MaterialMapIndex = null;
                            variableChanged = true;
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("view"))
                        {
                            var fileData = _editorControllerData.MaterialPackage.GetFile(FileType.Image, currentValue);

                            ImageOpenRequest?.Invoke(currentValue, fileData);
                        }
                    }
                }
            }

            return variableChanged;
        }
    }
}