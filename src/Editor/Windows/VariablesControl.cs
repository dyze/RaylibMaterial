using System.Numerics;
using System.Xml.Linq;
using Editor.Helpers;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using Library.Lighting;
using NLog;

namespace Editor.Windows
{
    class VariablesControl
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly EditorControllerData _editorControllerData;
        private readonly Dictionary<Type, Func<CodeVariableBase, string, bool>> _handlers;

        public VariablesControl(EditorControllerData editorControllerData)
        {
            _handlers = new()
            {
                { typeof(CodeVariableVector3), HandleVector3 },
                { typeof(CodeVariableVector4), HandleVector4 },
                { typeof(CodeVariableMatrix4x4), HandleMatrix4x4 },
                { typeof(CodeVariableFloat), HandleFloat },
                { typeof(CodeVariableTexture), HandleTexture },
                { typeof(CodeVariableColor), HandleColor },
                { typeof(CodeVariableLight), HandleLight },
                { typeof(CodeVariableUnsupported), HandleUnsupported },
            };
            this._editorControllerData = editorControllerData;
        }

        /// <summary>
        /// Render variables
        /// </summary>
        /// <returns>true if variables changed</returns>
        public bool Render(Dictionary<string, CodeVariableBase> variables)
        {
            var atLeastAVariableChanged = false;

            ImGui.SeparatorText("Variables");

            if (variables.Count == 0)
                ImGui.TextDisabled("Empty");
            else
                foreach (var (name, variable) in variables)
                {
                    ImGui.BeginGroup();

                    if (_handlers.TryGetValue(variable.GetType(), out var handler))
                    {
                        variable.SendToShader = handler(variable, name);

                        atLeastAVariableChanged |= variable.SendToShader;
                    }
                    else
                    {
                        ImGui.LabelText(name, variable.GetType().ToString());
                    }

                    ImGui.EndGroup();
                }

            return atLeastAVariableChanged;
        }

        private static bool HandleFloat(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = (variable as CodeVariableFloat).Value;

            if (ImGui.InputFloat(name, ref currentValue))
            {
                (variable as CodeVariableFloat).Value = currentValue;
                variableChanged = true;
            }

            ImGui.EndDisabled();

            return variableChanged;
        }

        private bool HandleVector3(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = (variable as CodeVariableVector3).Value;

            if (ImGui.InputFloat3(name, ref currentValue))
            {
                (variable as CodeVariableVector3).Value = currentValue;
                variableChanged = true;
            }

            ImGui.EndDisabled();

            return variableChanged;
        }

        private static bool HandleVector4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = (variable as CodeVariableVector4).Value;
            if (ImGui.InputFloat4(name, ref currentValue))
            {
                (variable as CodeVariableVector4).Value = currentValue;
                variableChanged = true;
            }

            ImGui.EndDisabled();

            return variableChanged;
        }

        private static bool HandleMatrix4x4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;


            var matrix4X4 = (variable as CodeVariableMatrix4x4).Value;

            var currentValue = matrix4X4;

            if (ImGui.TreeNode(name))
            {
                ImGui.BeginDisabled(variable.Internal);
                {
                    var row1 = new Vector4(currentValue.M11, currentValue.M12, currentValue.M13,
                        currentValue.M14);

                    if (ImGui.InputFloat4($"{name} row1", ref row1))
                    {
                        matrix4X4.M11 = row1.X;
                        matrix4X4.M12 = row1.Y;
                        matrix4X4.M13 = row1.Z;
                        matrix4X4.M14 = row1.W;
                        variableChanged = true;
                    }
                }

                {
                    var row2 = new Vector4(currentValue.M21, currentValue.M22, currentValue.M23,
                        currentValue.M24);

                    if (ImGui.InputFloat4($"{name} row2", ref row2))
                    {
                        matrix4X4.M21 = row2.X;
                        matrix4X4.M22 = row2.Y;
                        matrix4X4.M23 = row2.Z;
                        matrix4X4.M24 = row2.W;
                        variableChanged = true;
                    }
                }

                {
                    var row3 = new Vector4(currentValue.M31, currentValue.M32, currentValue.M33,
                        currentValue.M34);

                    if (ImGui.InputFloat4($"{name} row3", ref row3))
                    {
                        matrix4X4.M31 = row3.X;
                        matrix4X4.M32 = row3.Y;
                        matrix4X4.M33 = row3.Z;
                        matrix4X4.M34 = row3.W;
                        variableChanged = true;
                    }
                }

                {
                    var row4 = new Vector4(currentValue.M41, currentValue.M42, currentValue.M43,
                        currentValue.M44);

                    if (ImGui.InputFloat4($"{name} row4", ref row4))
                    {
                        matrix4X4.M41 = row4.X;
                        matrix4X4.M42 = row4.Y;
                        matrix4X4.M43 = row4.Z;
                        matrix4X4.M44 = row4.W;
                        variableChanged = true;
                    }
                }
                ImGui.EndDisabled();
                ImGui.TreePop();
            }

            return variableChanged;
        }

        private bool HandleColor(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = TypeConverters.ColorToVector4((variable as CodeVariableColor).Value);

            if (ImGui.ColorEdit4(name, ref currentValue))
            {
                (variable as CodeVariableColor).Value = TypeConverters.Vector4ToColor(currentValue);
                variableChanged = true;
            }

            ImGui.EndDisabled();

            return variableChanged;
        }

        private bool HandleTexture(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

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

            ImGui.EndDisabled();

            return variableChanged;
        }

        private bool HandleLight(CodeVariableBase variable, string name)
        {
            var variableChanged = false;


            if (ImGui.TreeNode(name))
            {
                var i = 0;
                foreach (var light in _editorControllerData.Lights)
                {
                    if (ImGui.TreeNode($"light[{i}]"))
                    {
                        ImGui.BeginDisabled(variable.Internal);

                        ImGui.Checkbox("Enabled", ref light.Enabled);

                        ImGui.InputFloat3("Position", ref light.Position);

                        ImGui.InputFloat3("Target", ref light.Target);

                        var currentValue = TypeConvertors.ColorToVector4(light.Color);
                        ImGui.ColorEdit4("Color", ref currentValue);

                        ImGui.LabelText("Type", light.Type.ToString());

                        ImGui.EndDisabled();
                        ImGui.TreePop();
                    }

                    i++;
                }

                ImGui.TreePop();
            }

            return variableChanged;
        }

        private bool HandleUnsupported(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            ImGui.LabelText(name, "unsupported");

            ImGui.EndDisabled();

            return variableChanged;
        }
    }
}