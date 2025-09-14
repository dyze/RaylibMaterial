using ImGuiNET;
using System.Numerics;
using Library;

namespace Editor.Windows
{
    static class VariablesWindow
    {
        /// <summary>
        /// Render variables
        /// </summary>
        /// <param name="shaderCodes"></param>
        /// <returns>true if variables changed</returns>
        public static bool Render(Dictionary<string, CodeVariable> variables)
        {
            var variableChanged = false;

            if (ImGui.Begin("Variables"))
            {
                foreach (var (name, variable) in variables)
                {
                   
                    if (variable.Type == typeof(Vector4))
                    {
                        var currentValue = (Vector4)variable.Value;
                        if (ImGui.InputFloat4(name, ref currentValue))
                        {
                            variable.Value = currentValue;
                            variableChanged = true;
                        }
                    }
                    else if (variable.Type == typeof(float))
                    {
                        var currentValue = (float)variable.Value;
                        if (ImGui.InputFloat(name, ref currentValue))
                        {
                            variable.Value = currentValue;
                            variableChanged = true;
                        }
                    }
                    else if (variable.Type == typeof(string))
                    {
                        var currentValue = (string)variable.Value;
                        if (ImGui.InputText(name, ref currentValue, 200))
                        {
                            variable.Value = currentValue;
                            variableChanged = true;
                        }
                    }
                    else
                    {
                        ImGui.LabelText(name, variable.Type.ToString());
                    }
                }
            }

            ImGui.End();
            return variableChanged;
        }
    }
}