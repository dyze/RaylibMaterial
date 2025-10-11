using System.Numerics;
using Editor.EditorControllerNS;
using ImGuiNET;
using Library.CodeVariable;
using Library.Helpers;
using NLog;

namespace Editor.Ui.Windows.VariablesControls;

partial class VariablesControls
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly EditorControllerData _editorControllerData;
    private readonly Dictionary<Type, Func<CodeVariableBase, string, bool>> _handlers;
    private readonly Dictionary<string, Action> _handlersForInternals;

    public VariablesControls(EditorControllerData editorControllerData)
    {
        _handlers = new()
        {
            // ints
            { typeof(CodeVariableInt), (v, s) => HandleInt(v as CodeVariableInt, s) },
            { typeof(CodeVariableIVector2), (v, s) => HandleIVector2(v as CodeVariableIVector2, s) },
            { typeof(CodeVariableIVector3), (v, s) => HandleIVector3(v as CodeVariableIVector3, s) },
            { typeof(CodeVariableIVector4), (v, s) => HandleIVector4(v as CodeVariableIVector4, s) },
            /// uints
            { typeof(CodeVariableUInt), (v, s) => HandleUInt(v as CodeVariableUInt, s) },
            { typeof(CodeVariableUVector2), (v, s) => HandleUVector2(v as CodeVariableUVector2, s) },
            { typeof(CodeVariableUVector3), (v, s) => HandleUVector3(v as CodeVariableUVector3, s) },
            { typeof(CodeVariableUVector4), (v, s) => HandleUVector4(v as CodeVariableUVector4, s) },
            /// floats
            { typeof(CodeVariableFloat),  (v, s) => HandleFloat(v as CodeVariableFloat, s) },
            { typeof(CodeVariableVector2), (v, s) => HandleVector2(v as CodeVariableVector2, s) },
            { typeof(CodeVariableVector3), (v, s) => HandleVector3(v as CodeVariableVector3, s) },
            { typeof(CodeVariableVector4), (v, s) => HandleVector4(v as CodeVariableVector4, s) },
            ///
            { typeof(CodeVariableMatrix4x4), (v, s) => HandleMatrix4x4(v as CodeVariableMatrix4x4, s) },

            { typeof(CodeVariableTexture), (v, s) => HandleTexture(v as CodeVariableTexture, s) },
            { typeof(CodeVariableColor), (v, s) => HandleColor(v as CodeVariableColor, s) },
            { typeof(CodeVariableInternal), HandleInternal },
            { typeof(CodeVariableUnsupported), HandleUnsupported },
        };
        _handlersForInternals = new()
        {
            { "lights", HandleLights },
        };
        _editorControllerData = editorControllerData;
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

                    var internallyHandled = variable is CodeVariableInternal;

                    var flags = internallyHandled == false
                        ? ImGuiTreeNodeFlags.DefaultOpen
                        : ImGuiTreeNodeFlags.None;

                    var nameToDisplay = internallyHandled ? $"{name} (internal)" : name;

                    if (ImGui.TreeNodeEx(nameToDisplay, flags))
                    {
                        ImGui.BeginDisabled(internallyHandled);
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

    private static bool HandleMatrix4x4(CodeVariableMatrix4x4 variable, string name)
    {
        var variableChanged = false;

        var matrix4X4 = variable.Value;

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

    private bool HandleColor(CodeVariableColor variable, string name)
    {
        var currentValue = TypeConverters.ColorToVector4(variable.Value);

        var variableChanged = ImGui.ColorEdit4($"##{name}", ref currentValue);
        if (variableChanged)
            variable.Value = TypeConverters.Vector4ToColor(currentValue);

        return variableChanged;
    }

    private bool HandleInternal(CodeVariableBase variable, string name)
    {
        if (_handlersForInternals.TryGetValue(name, out var handler))
            handler();
        else
            ImGui.Text("This variable is fed internally, there is no available output");

        return false;
    }

    private void HandleLights()
    {
        var i = 0;
        foreach (var light in _editorControllerData.Lights)
        {
            if (ImGui.TreeNodeEx($"lights[{i}]", ImGuiTreeNodeFlags.DefaultOpen))
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
    }

    private bool HandleUnsupported(CodeVariableBase variable, string name)
    {
        ImGui.LabelText($"##{name}", "unsupported");
        return false;
    }
}