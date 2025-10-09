using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Ui.Windows.VariablesControls
{
    partial class VariablesControls
    {
        private static bool HandleFloat(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableFloat).Value;

            return ImGui.InputFloat($"##{name}", ref currentValue, 0.01f, 0.1f);
        }

        private bool HandleVector2(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableVector2).Value;

            return ImGui.InputFloat2($"##{name}", ref currentValue);
        }

        private bool HandleVector3(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableVector3).Value;

            return ImGui.InputFloat3($"##{name}", ref currentValue);
        }

        private static bool HandleVector4(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableVector4).Value;
            return ImGui.InputFloat4($"##{name}", ref currentValue);
        }
    }
}