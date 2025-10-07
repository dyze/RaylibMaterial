using ImGuiNET;
using Library.CodeVariable;
using NLog.LayoutRenderers;

namespace Editor.Windows
{
    partial class VariablesControls
    {
        private bool HandleUInt(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (int)(variable as CodeVariableUInt).Value;

            if (ImGui.InputInt($"##{name}", ref currentValue))
            {
                (variable as CodeVariableUInt).Value = (uint)currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleUiVector2(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUiVector2).Value;
            var temp = new int[] { (int)currentValue[0], (int)currentValue[1] };

            if (ImGui.InputInt2($"##{name}", ref temp[0]))
            {
                //(variable as CodeVariableUiVector2).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleUiVector3(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUiVector3).Value;
            var temp = new int[] { (int)currentValue[0], (int)currentValue[1], (int)currentValue[2] };

            if (ImGui.InputInt3($"##{name}", ref temp[0]))
            {
                //(variable as CodeVariableUiVector3).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleUiVector4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUiVector4).Value;
            var temp = new int[] { (int)currentValue[0], (int)currentValue[1], (int)currentValue[2], (int)currentValue[3] };

            if (ImGui.InputInt4($"##{name}", ref temp[0]))
            {
                //(variable as CodeVariableUiVector4).Value = currentValue;
                variableChanged = true;
            }

            return variableChanged;
        }
    }
}