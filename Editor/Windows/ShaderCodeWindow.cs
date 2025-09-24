using ImGuiNET;
using System.Numerics;
using Library;

namespace Editor.Windows
{
    /// <summary>
    /// Handles the display and the modification of code
    /// </summary>
    internal class ShaderCodeWindow
    {
        public event Action? BuildPressed;

        /// <summary>
        /// Render Shader codes
        /// </summary>
        /// <param name="shaderCodes"></param>
        /// <returns>code changed or not</returns>
        public bool Render(Dictionary<string, ShaderCode> shaderCodes)
        {
            var codeChanged = false;

            if (ImGui.Begin("Code"))
            {
                var needsRebuild = false;
                var isValid = false;
                foreach (var (_, code) in shaderCodes)
                {
                    needsRebuild |= code.NeedsRebuild;
                    isValid |= code.IsValid;
                }

                ImGui.BeginDisabled(needsRebuild == false);
                if (ImGui.Button("Build"))
                    BuildPressed?.Invoke();
                ImGui.EndDisabled();


                if (isValid == false)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1f, 0, 0, 1f),
                        "not valid");
                }

                var flags = ImGuiTabBarFlags.None;

                if (ImGui.BeginTabBar("MyTabBar", flags))
                {
                    foreach (var (key, code) in shaderCodes)
                        codeChanged |= RenderTab(key, code);

                    ImGui.EndTabBar();
                }
            }
            ImGui.End();


            return codeChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="code"></param>
        /// <returns>code changed or not</returns>
        private bool RenderTab(string key, ShaderCode code)
        {
            var codeChanged = false;

            var name = key;
            if (code.NeedsRebuild)
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
                    code.NeedsRebuild = true;
                    codeChanged = true;
                }

                ImGui.EndTabItem();
            }

            return codeChanged;
        }
    }

}