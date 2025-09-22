using Editor.Helpers;
using ImGuiNET;
using Library;
using Library.Helpers;
using NLog;
using System.Drawing;
using System.Numerics;
using System.Xml.Linq;

namespace Editor.Windows
{
    class VariablesControl
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly EditorControllerData editorControllerData;
        private Dictionary<Type, Func<CodeVariable, string, bool, bool>> _handlers;

        public VariablesControl(EditorControllerData editorControllerData)
        {
            _handlers = new()
        {
            { typeof(Vector4), HandleVector4 },
            { typeof(float), HandleFloat },
            { typeof(string), HandleString },
            { typeof(System.Drawing.Color), HandleColor },
        };
            this.editorControllerData = editorControllerData;
        }



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

                    if(_handlers.TryGetValue(variable.Type, out var handler))
                    {
                        variableChanged = handler(variable, name, variableChanged);
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
            if (variable.Value == null)
                throw new NullReferenceException("variable.Value is null");

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
            if (variable.Value == null)
                throw new NullReferenceException("variable.Value is null");

            var currentValue = (Vector4)variable.Value;
            if (ImGui.InputFloat4(name, ref currentValue))
            {
                variable.Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleColor(CodeVariable variable, string name, bool variableChanged)
        {
            if (variable.Value == null)
                throw new NullReferenceException("variable.Value is null");

            var currentValue = TypeConvertors.ColorToVec4((Color)variable.Value);
            if (ImGui.ColorEdit4(name, ref currentValue))
            {
                variable.Value = TypeConvertors.Vec4ToColor(currentValue);
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleString(CodeVariable variable, string name, bool variableChanged)
        {
            if (variable.Value == null)
                throw new NullReferenceException("variable.Value is null");

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