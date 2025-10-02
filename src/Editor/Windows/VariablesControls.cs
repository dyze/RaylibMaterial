using System.Numerics;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using NLog;

namespace Editor.Windows
{
    partial class VariablesControls
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly EditorControllerData _editorControllerData;
        private readonly Dictionary<Type, Func<CodeVariableBase, string, bool>> _handlers;

        public VariablesControls(EditorControllerData editorControllerData)
        {
            _handlers = new()
            {
                { typeof(CodeVariableInt), HandleInt },
                { typeof(CodeVariableVector2), HandleVector2 },
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

            if (ImGui.CollapsingHeader("Variables", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.SameLine();
                HelpMarker.Run("Variables that are necessary for the shader to run");

                if (variables.Count == 0)
                    ImGui.TextDisabled("Empty");
                else
                {
                    var sortedVariables = variables.OrderBy(e => e.Key);
                    foreach (var (name, variable) in sortedVariables)
                    {
                        ImGui.PushID(name);

                        ImGuiTreeNodeFlags flags = variable.Internal == false
                            ? ImGuiTreeNodeFlags.DefaultOpen
                            : ImGuiTreeNodeFlags.None;

                        if (ImGui.TreeNodeEx(name, flags))
                        {
                            ImGui.BeginDisabled(variable.Internal);
                            ImGui.BeginGroup();

                            if (_handlers.TryGetValue(variable.GetType(), out var handler))
                            {
                                var sendToShader = handler(variable, name);
                                if (sendToShader)
                                {
                                    // Don't delete previous value because maybe not yet applied by controller
                                    Logger.Trace($"{name}: SendToShader");
                                    variable.SendToShader = sendToShader;
                                }

                                atLeastAVariableChanged |= sendToShader;
                            }
                            else
                            {
                                ImGui.LabelText(name, variable.GetType().ToString());
                            }

                            ImGui.EndDisabled();
                            ImGui.EndGroup();

                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort | ImGuiHoveredFlags.NoSharedDelay))
                            {
                                var description = TypeConvertors.GetUniformDescription(name);
                                if (description != null)
                                    ImGui.SetTooltip(description.Description);
                            }

                            ImGui.TreePop();
                        }

                        ImGui.PopID();
                    }
                }
            }

            return atLeastAVariableChanged;
        }


        private bool HandleInt(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableInt).Value;

            if (ImGui.InputInt($"##{name}", ref currentValue))
            {
                (variable as CodeVariableInt).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleFloat(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableFloat).Value;

            if (ImGui.InputFloat($"##{name}", ref currentValue, 0.01f, 0.1f))
            {
                (variable as CodeVariableFloat).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleMatrix4x4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var matrix4X4 = (variable as CodeVariableMatrix4x4).Value;

            var currentValue = matrix4X4;


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

            return variableChanged;
        }

        private bool HandleColor(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = TypeConverters.ColorToVector4((variable as CodeVariableColor).Value);

            if (ImGui.ColorEdit4($"##{name}", ref currentValue))
            {
                (variable as CodeVariableColor).Value = TypeConverters.Vector4ToColor(currentValue);
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleLight(CodeVariableBase variable, string name)
        {
            const bool variableChanged = false;

            var i = 0;
            foreach (var light in _editorControllerData.Lights)
            {
                if (ImGui.TreeNodeEx($"light[{i}]", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Checkbox("Enabled", ref light.Enabled);

                    ImGui.LabelText("Type", light.Type.ToString());

                    ImGui.InputFloat3("Position", ref light.Position);

                    ImGui.InputFloat3("Target", ref light.Target);

                    var currentValue = TypeConvertors.ColorToVector4(light.Color);
                    ImGui.ColorEdit4("Color", ref currentValue);

                    ImGui.InputFloat("Intensity", ref light.Intensity);

                    ImGui.TreePop();
                }

                i++;
            }


            return variableChanged;
        }

        private bool HandleUnsupported(CodeVariableBase variable, string name)
        { 
            ImGui.LabelText($"##{name}", "unsupported");
            return false;
        }
    }
}