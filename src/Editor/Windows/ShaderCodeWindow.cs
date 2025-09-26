using System.Drawing;
using ImGuiNET;
using System.Numerics;
using Editor.Configuration;
using Library;
using Library.Helpers;
using Library.Packaging;

namespace Editor.Windows
{
    /// <summary>
    /// Handles the display and the modification of code
    /// </summary>
    internal class ShaderCodeWindow(EditorConfiguration editorConfiguration,
        EditorControllerData editorControllerData)
    {
        public event Action? BuildPressed;

        /// <summary>
        /// Render Shader codes
        /// </summary>
        /// <param name="shaderCodes"></param>
        /// <returns>code changed or not</returns>
        public bool Render(Dictionary<FileId, ShaderCode> shaderCodes)
        {
            var codeChanged = false;

            editorControllerData.UpdateWindowPosAndSize(EditorControllerData.WindowId.Code);

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


                ImGui.SameLine();
                if(isValid)
                    ImGui.TextColored(TypeConvertors.ColorToVector4(Color.LimeGreen), "valid");
                else
                    ImGui.TextColored(TypeConvertors.ColorToVector4(Color.Red), "not valid");

                var flags = ImGuiTabBarFlags.None;

                if (ImGui.BeginTabBar("MyTabBar", flags))
                {
                    foreach (var (fileId, code) in shaderCodes)
                        codeChanged |= RenderTab(fileId, code);

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
        private bool RenderTab(FileId fileId, ShaderCode code)
        {
            var codeChanged = false;

            var name = fileId.FileName;
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