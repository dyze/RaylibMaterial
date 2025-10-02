using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Windows
{
    partial class VariablesControls
    {
        private bool HandleVector2(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableVector2).Value;

            if (ImGui.InputFloat2($"##{name}", ref currentValue))
            {
                (variable as CodeVariableVector2).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleVector3(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableVector3).Value;

            if (ImGui.InputFloat3($"##{name}", ref currentValue))
            {
                (variable as CodeVariableVector3).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleVector4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableVector4).Value;
            if (ImGui.InputFloat4($"##{name}", ref currentValue))
            {
                (variable as CodeVariableVector4).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }
    }
}