using ImGuiNET;
using Library.CodeVariable;

namespace Editor.Ui.Windows.VariablesControls
{
    partial class VariablesControls
    {
        private bool HandleUInt(CodeVariableUInt variable, string name)
        {
            var currentValue = (int)variable.Value;

            var variableChanged = ImGui.InputInt($"##{name}", ref currentValue, 1, 10);
            if (variableChanged)
                variable.Value = (uint)currentValue;

            return variableChanged;
        }

        private bool HandleUVector2(CodeVariableUVector2 variable, string name)
        {
            var currentValue = variable.Value;
            var temp = new[] { (int)currentValue.X, (int)currentValue.Y};

            var variableChanged = ImGui.InputInt2($"##{name}", ref temp[0]);
            if (variableChanged)
            {
                currentValue.X = (uint)temp[0];
                currentValue.Y = (uint)temp[1];
            }

            return variableChanged;
        }

        private bool HandleUVector3(CodeVariableUVector3 variable, string name)
        {
            var currentValue = variable.Value;
            var temp = new[] { (int)currentValue.X, (int)currentValue.Y, (int)currentValue.Z };

            var variableChanged = ImGui.InputInt3($"##{name}", ref temp[0]);
            if (variableChanged)
            {
                currentValue.X = (uint)temp[0];
                currentValue.Y = (uint)temp[1];
                currentValue.Z = (uint)temp[2];
            }

            return variableChanged;
        }

        private static bool HandleUVector4(CodeVariableUVector4 variable, string name)
        {
            var currentValue = variable.Value;
            var temp = new[] { (int)currentValue.X, (int)currentValue.Y, (int)currentValue.Z, (int)currentValue.W };

            var variableChanged = ImGui.InputInt4($"##{name}", ref temp[0]);
            if (variableChanged)
            {
                currentValue.X = (uint)temp[0];
                currentValue.Y = (uint)temp[1];
                currentValue.Z = (uint)temp[2];
                currentValue.W = (uint)temp[3];
            }

            return variableChanged;
        }
    }
}