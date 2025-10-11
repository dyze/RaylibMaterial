using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Ui.Windows.VariablesControls
{
    partial class VariablesControls
    {
        private bool HandleInt(CodeVariableInt variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputInt($"##{name}", ref currentValue, 1, 10);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }

        private bool HandleIVector2(CodeVariableIVector2 variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputInt2($"##{name}", ref currentValue.X);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }

        private bool HandleIVector3(CodeVariableIVector3 variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputInt3($"##{name}", ref currentValue.X);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }

        private static bool HandleIVector4(CodeVariableIVector4 variable, string name)
        {
            var currentValue = variable.Value;

            var variableChanged = ImGui.InputInt4($"##{name}", ref currentValue.X);
            if (variableChanged)
                variable.Value = currentValue;

            return variableChanged;
        }
    }
}