using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Ui.Windows.VariablesControls
{
    partial class VariablesControls
    {
        private bool HandleUInt(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUInt).Value;
            var temp = (int)currentValue;

            if (ImGui.InputInt($"##{name}", ref temp))
            {
                (variable as CodeVariableUInt).Value = (uint)temp; 
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleUVector2(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUVector2).Value;
            var temp = new int[] { (int)currentValue[0], (int)currentValue[1] };

            if (ImGui.InputInt2($"##{name}", ref temp[0]))
            {
                for (var i = 0; i < temp.Length; i++)
                    currentValue[i] = (uint)temp[i];
                variableChanged = true;
            }

            return variableChanged;
        }

        private bool HandleUVector3(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUVector3).Value;
            var temp = new int[] { (int)currentValue[0], (int)currentValue[1], (int)currentValue[2] };

            if (ImGui.InputInt3($"##{name}", ref temp[0]))
            {
                for (var i = 0; i < temp.Length; i++)
                    currentValue[i] = (uint)temp[i];
                variableChanged = true;
            }

            return variableChanged;
        }

        private static bool HandleUVector4(CodeVariableBase variable, string name)
        {
            var variableChanged = false;

            var currentValue = (variable as CodeVariableUVector4).Value;
            var temp = new int[] { (int)currentValue[0], (int)currentValue[1], (int)currentValue[2], (int)currentValue[3] };

            if (ImGui.InputInt4($"##{name}", ref temp[0]))
            {
                for (var i = 0; i < temp.Length; i++)
                    currentValue[i] = (uint)temp[i];
                variableChanged = true;
            }

            return variableChanged;
        }
    }
}