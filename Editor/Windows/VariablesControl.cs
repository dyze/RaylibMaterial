using Editor.Helpers;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using NLog;

namespace Editor.Windows
{
    class VariablesControl
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly EditorControllerData editorControllerData;
        private Dictionary<Type, Func<CodeVariableBase, string, bool, bool>> _handlers;

        public VariablesControl(EditorControllerData editorControllerData)
        {
            _handlers = new()
            {
                { typeof(CodeVariableVector4), HandleVector4 },
                { typeof(CodeVariableFloat), HandleFloat },
                { typeof(CodeVariableTexture), HandleTexture },
                { typeof(CodeVariableColor), HandleColor },
            };
                this.editorControllerData = editorControllerData;
            }



        /// <summary>
        /// Render variables
        /// </summary>
        /// <returns>true if variables changed</returns>
        public bool Render(Dictionary<string, CodeVariableBase> variables)
        {
            var variableChanged = false;

            ImGui.SeparatorText("Variables");

            if (variables.Count == 0)
                ImGui.TextDisabled("Empty");
            else
                foreach (var (name, variable) in variables)
                {
                    ImGui.BeginGroup();

                    if (_handlers.TryGetValue(variable.GetType(), out var handler))
                    {
                        variableChanged = handler(variable, name, variableChanged);
                    }
                    else
                    {
                        ImGui.LabelText(name, variable.GetType().ToString());
                    }

                    ImGui.EndGroup();
                }

            return variableChanged;
        }

        private static bool HandleFloat(CodeVariableBase variable, string name, bool variableChanged)
        {
            var currentValue = (variable as CodeVariableFloat).Value;
            if (ImGui.InputFloat(name, ref currentValue))
            {
                (variable as CodeVariableFloat).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleVector4(CodeVariableBase variable, string name, bool variableChanged)
        {
            var currentValue = (variable as CodeVariableVector4).Value;
            if (ImGui.InputFloat4(name, ref currentValue))
            {
                (variable as CodeVariableVector4).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleColor(CodeVariableBase variable, string name, bool variableChanged)
        {
            var currentValue = TypeConvertors.ColorToVector4((variable as CodeVariableColor).Value);
            if (ImGui.ColorEdit4(name, ref currentValue))
            {
                (variable as CodeVariableColor).Value = TypeConvertors.Vec4ToColor(currentValue);
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleTexture(CodeVariableBase variable, string name, bool variableChanged)
        {
            var currentValue = (variable as CodeVariableTexture).Value;
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
                    var draggedRelativeFilePath = editorControllerData.DataFileExplorerData.DraggedRelativeFilePath;
                    Logger.Trace($"dropped {draggedRelativeFilePath}");

                    var draggedFileName = editorControllerData.DataFileExplorerData.DraggedFileName;
                    editorControllerData.MaterialPackage.AddFile(draggedFileName,
                        editorControllerData.DataFileExplorerData.DataFolder.ReadBinaryFile(draggedRelativeFilePath));

                    (variable as CodeVariableTexture).Value = draggedFileName;
                    variableChanged = true;

                    editorControllerData.DataFileExplorerData.DraggedFullFilePath = "";
                    editorControllerData.DataFileExplorerData.DraggedFileName = "";
                }

                ImGui.EndDragDropTarget();
            }

            return variableChanged;
        }
    }
}