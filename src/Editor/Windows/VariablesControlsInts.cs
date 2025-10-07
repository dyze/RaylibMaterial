using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Windows
{
    partial class VariablesControls
    {
        private bool HandleInt(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableInt).Value;

            return ImGui.InputInt($"##{name}", ref currentValue);
        }

        private bool HandleIVector2(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableIVector2).Value;
            
            return ImGui.InputInt2($"##{name}", ref currentValue[0]);
        }

        private bool HandleIVector3(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableIVector3).Value;

            return ImGui.InputInt3($"##{name}", ref currentValue[0]);
        }

        private static bool HandleIVector4(CodeVariableBase variable, string name)
        {
            var currentValue = (variable as CodeVariableIVector4).Value;
            return ImGui.InputInt4($"##{name}", ref currentValue[0]);
        }
    }
}