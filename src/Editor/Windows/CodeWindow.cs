using System.Drawing;
using ImGuiNET;
using System.Numerics;
using Editor.Configuration;
using Library;
using Library.Packaging;

namespace Editor.Windows
{
    /// <summary>
    /// Handles the display and the modification of code
    /// </summary>
    internal class CodeWindow(
        EditorConfiguration editorConfiguration,
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

                //ImGui.BeginDisabled(needsRebuild == false);
                if (ImGui.Button("Build"))
                    BuildPressed?.Invoke();
                // ImGui.EndDisabled();


                ImGui.SameLine();
                if (isValid == false)
                    ImGui.TextColored(TypeConverters.ColorToVector4(Color.Red), "not valid, check messages");
                else
                if (needsRebuild)
                    ImGui.TextColored(TypeConverters.ColorToVector4(Color.Orange), "needs rebuild");
                else
                    ImGui.TextColored(TypeConverters.ColorToVector4(Color.LimeGreen), "valid");


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
        /// <param name="fileId"></param>
        /// <param name="code"></param>
        /// <returns>code changed or not</returns>
        private bool RenderTab(FileId fileId, ShaderCode code)
        {
            var codeChanged = false;

            var name = fileId.FileName;
            var styleAttributesPushed = 0;
            if (code.IsValid == false)
            {
                ImGui.PushStyleColor(ImGuiCol.Tab, TypeConverters.ColorToVector4(Color.DarkRed));
                styleAttributesPushed++;
                ImGui.PushStyleColor(ImGuiCol.TabSelected, TypeConverters.ColorToVector4(Color.Red));
                styleAttributesPushed++;
                ImGui.PushStyleColor(ImGuiCol.TabHovered, TypeConverters.ColorToVector4(Color.IndianRed));
                styleAttributesPushed++;
            }
            else
            if (code.NeedsRebuild)
            {
                ImGui.PushStyleColor(ImGuiCol.Tab, TypeConverters.ColorToVector4(Color.DarkOrange));
                styleAttributesPushed++;
                ImGui.PushStyleColor(ImGuiCol.TabSelected, TypeConverters.ColorToVector4(Color.Orange));
                styleAttributesPushed++;
                ImGui.PushStyleColor(ImGuiCol.TabHovered, TypeConverters.ColorToVector4(Color.Yellow));
                styleAttributesPushed++;
            }


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

            if (styleAttributesPushed > 0)
                ImGui.PopStyleColor(styleAttributesPushed);


            return codeChanged;
        }
    }
}