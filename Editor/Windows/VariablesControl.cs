using ImGuiNET;
using System.Numerics;
using Library;
using NLog;

namespace Editor.Windows
{
    class VariablesControl(EditorControllerData editorControllerData)
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Render variables
        /// </summary>
        /// <returns>true if variables changed</returns>
        public bool Render(Dictionary<string, CodeVariable> variables)
        {
            var variableChanged = false;

            ImGui.SeparatorText("Variables");

            if (variables.Count == 0)
                ImGui.TextDisabled("Empty");
            else
                foreach (var (name, variable) in variables)
                {
                    ImGui.BeginGroup();
                    if (variable.Type == typeof(Vector4))
                    {
                        variableChanged = HandleVector4(variable, name, variableChanged);
                    }
                    else if (variable.Type == typeof(float))
                    {
                        variableChanged = HandleFloat(variable, name, variableChanged);
                    }
                    else if (variable.Type == typeof(string))
                    {
                        variableChanged = HandleString(variable, name, variableChanged);
                    }
                    else
                    {
                        ImGui.LabelText(name, variable.Type.ToString());
                    }

                    ImGui.EndGroup();


                }

            return variableChanged;
        }

        private static bool HandleFloat(CodeVariable variable, string name, bool variableChanged)
        {
            var currentValue = (float)variable.Value;
            if (ImGui.InputFloat(name, ref currentValue))
            {
                variable.Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleVector4(CodeVariable variable, string name, bool variableChanged)
        {
            var currentValue = (Vector4)variable.Value;
            if (ImGui.InputFloat4(name, ref currentValue))
            {
                variable.Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleString(CodeVariable variable, string name, bool variableChanged)
        {
            var currentValue = (string)variable.Value;
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
                    var draggedRelativeFilePath = editorControllerData.DataFileExplorerData.DraggedFullFilePath;
                    Logger.Trace($"dropped {draggedRelativeFilePath}");

                    var draggedFileName = editorControllerData.DataFileExplorerData.DraggedFileName;
                    editorControllerData.MaterialPackage.AddFile(draggedFileName,
                        editorControllerData.DataFileExplorerData.DataFolder.ReadBinaryFile(draggedRelativeFilePath));

                    editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
                    editorControllerData.DataFileExplorerData.DraggedFileName = "";
                }

                ImGui.EndDragDropTarget();
            }

            return variableChanged;
        }
    }
}