using System.Numerics;
using Editor.Helpers;
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

            ImGui.SeparatorText("Variables");

            if (variables.Count == 0)
                ImGui.TextDisabled("Empty");
            else
                foreach (var (name, variable) in variables)
                {
                    ImGui.PushID(name);

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

                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort | ImGuiHoveredFlags.NoSharedDelay))
                    {
                        var description = TypeConvertors.GetUniformDescription(name);
                        if (description != null)
                            ImGui.SetTooltip(description.Description);
                    }

                    ImGui.PopID();
                }

            return atLeastAVariableChanged;
        }


        private bool HandleInt(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = (variable as CodeVariableInt).Value;

            if (ImGui.InputInt(name, ref currentValue))
            {
                (variable as CodeVariableInt).Value = currentValue;
                variableChanged = true;
            }

            ImGui.EndDisabled();

            return variableChanged;
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

        private bool HandleVector2(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            ImGui.BeginDisabled(variable.Internal);

            var currentValue = (variable as CodeVariableVector2).Value;

            if (ImGui.InputFloat2(name, ref currentValue))
            {
                (variable as CodeVariableVector2).Value = currentValue;
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

                        ImGui.LabelText("Type", light.Type.ToString());

                        ImGui.InputFloat3("Position", ref light.Position);

                        ImGui.InputFloat3("Target", ref light.Target);

                        var currentValue = TypeConvertors.ColorToVector4(light.Color);
                        ImGui.ColorEdit4("Color", ref currentValue);

                        ImGui.InputFloat("Intensity", ref light.Intensity);

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