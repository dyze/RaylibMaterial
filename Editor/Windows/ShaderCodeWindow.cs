using ImGuiNET;
using System.Numerics;
using Library;

namespace Editor.Windows
{
    internal class ShaderCodeWindow
    {
        public event Action<ShaderCode>? ApplyChangesPressed;

        /// <summary>
        /// Render Shader codes
        /// </summary>
        /// <param name="shaderCodes"></param>
        public void Render(Dictionary<string, ShaderCode> shaderCodes)
        {
            if (ImGui.Begin("Code"))
            {
                var flags = ImGuiTabBarFlags.None;
                if (ImGui.BeginTabBar("MyTabBar", flags))
                {
                    foreach (var (key, code) in shaderCodes)
                        RenderTab(key, code);
                    
                    ImGui.EndTabBar();
                }

                ImGui.Separator();
            }
            ImGui.End();
        }

        private void RenderTab(string key, ShaderCode code)
        {
            var name = key;
            if (code.Modified)
                name += " *";
            if (ImGui.BeginTabItem(name))
            {

                var inputFlags = ImGuiInputTextFlags.AllowTabInput;
                if (ImGui.InputTextMultiline("##source",
                        ref code.Code,
                        20000,
                        new Vector2(-1, -1),
                        inputFlags))
                {
                    code.Modified = true;
                }

                if (code.Modified)
                    if (ImGui.Button("Apply"))
                    {
                        ApplyChangesPressed?.Invoke(code);
                    }

                if (code.IsValid == false)
                    ImGui.TextColored(new Vector4(1f, 0, 0, 1f),
                        "not valid");

                ImGui.EndTabItem();
            }
        }
    }
    
}