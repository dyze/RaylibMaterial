using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Windows
{
    partial class VariablesControls
    {
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

        private bool HandleIVector2(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableIVector2).Value;

            if (ImGui.InputInt2($"##{name}", ref currentValue[0]))
            {
                //(variable as CodeVariableIVector2).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleIVector3(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableIVector3).Value;

            if (ImGui.InputInt3($"##{name}", ref currentValue[0]))
            {
                //(variable as CodeVariableIVector3).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleIVector4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableIVector4).Value;
            if (ImGui.InputInt4($"##{name}", ref currentValue[0]))
            {
                //(variable as CodeVariableIVector4).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }
    }
}