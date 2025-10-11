using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Ui.Windows.VariablesControls
{
    partial class VariablesControls
    {
        private static bool HandleFloat(CodeVariableFloat variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputFloat($"##{name}", ref currentValue, 0.01f, 0.1f);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }

        private bool HandleVector2(CodeVariableVector2 variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputFloat2($"##{name}", ref currentValue);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }

        private bool HandleVector3(CodeVariableVector3 variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputFloat3($"##{name}", ref currentValue);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }

        private static bool HandleVector4(CodeVariableVector4 variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputFloat4($"##{name}", ref currentValue);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }
    }
}