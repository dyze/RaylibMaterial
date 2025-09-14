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
        public static bool Render(Dictionary<string, ShaderCode> shaderCodes)
        {
            var variableChanged = false;

            if (ImGui.Begin("Variables"))
            {
                foreach (var (_, code) in shaderCodes)
                {
                    foreach (var variable in code.Variables)
                    {
                        ImGui.LabelText(variable.Name, variable.Type.ToString());
                        if (variable.Type == typeof(Vector4))
                        {
                            var currentValue = (Vector4)variable.Value;
                            if (ImGui.InputFloat4(variable.Name, ref currentValue))
                            {
                                variable.Value = currentValue;
                                variableChanged = true;
                            }
                        }
                        else if (variable.Type == typeof(float))
                        {
                            var currentValue = (float)variable.Value;
                            if (ImGui.InputFloat(variable.Name, ref currentValue))
                            {
                                variable.Value = currentValue;
                                variableChanged = true;
                            }
                        }
                        else if (variable.Type == typeof(string))
                        {
                            var currentValue = (string)variable.Value;
                            if (ImGui.InputText(variable.Name, ref currentValue, 200))
                            {
                                variable.Value = currentValue;
                                variableChanged = true;
                            }
                        }
                    }
                }
            }

            ImGui.End();
            return variableChanged;
        }
    }
    
}