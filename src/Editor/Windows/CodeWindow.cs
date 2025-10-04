using System.Drawing;
using ImGuiNET;
using Editor.Configuration;
using Library;
using Library.Packaging;
using ImGuiColorTextEditNet;

namespace Editor.Windows
{
    /// <summary>
    /// Handles the display and the modification of code
    /// </summary>
    internal class CodeWindow
    {
        private readonly EditorControllerData _editorControllerData;

        private Dictionary<FileId, TextEditor> _textEditors = [];

        /// <summary>
        /// Handles the display and the modification of code
        /// </summary>
        public CodeWindow(EditorConfiguration editorConfiguration,
            EditorControllerData editorControllerData)
        {
            _editorControllerData = editorControllerData;
        }

        public event Action? BuildPressed;

        /// <summary>
        /// Render Shader codes
        /// </summary>
        /// <param name="shaderCodes"></param>
        /// <returns>code changed or not</returns>
        public bool Render(Dictionary<FileId, ShaderCode> shaderCodes)
        {
            var codeChanged = false;

            foreach (var (key, code) in shaderCodes)
            {
                if (_textEditors.ContainsKey(key) == false)
                {
                    var currentTextWithLineFeeds = code.Code.ReplaceLineEndings("\n");

                    var editor = new TextEditor()
                    {
                        AllText = currentTextWithLineFeeds,
                        SyntaxHighlighter = new GlSlStyleHighlighter()
                    };
                    _textEditors.Add(key, editor);
                }
            }

            foreach (var key in _textEditors.Keys)
            {
                if (shaderCodes.ContainsKey(key) == false)
                    _textEditors.Remove(key);
            }


            _editorControllerData.UpdateWindowPosAndSize(EditorControllerData.WindowId.Code);

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
                else if (needsRebuild)
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
            else if (code.NeedsRebuild)
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
                var textEditor = _textEditors[fileId];
                ImGui.Text(
                    $"Cur:{textEditor.CursorPosition} SEL: {textEditor.Selection.Start} - {textEditor.Selection.End}"
                );

                // We force line endings to \n to be able to properly detect modifications on text
                var currentTextWithLineFeeds = code.Code.ReplaceLineEndings("\n");
                //textEditor.AllText = currentTextWithLineFeeds;

                //var demoErrors = new Dictionary<int, object>
                //{
                //    { 1, "Syntax error etc 1" },
                //    { 10, "Syntax error etc 10" }
                //};
                //textEditor.ErrorMarkers.SetErrorMarkers(demoErrors);

                textEditor.Render("EditWindow");

                var newTextWithLineFeeds = textEditor.AllText.ReplaceLineEndings("\n");

                if (newTextWithLineFeeds != currentTextWithLineFeeds)
                {
                    //var src = newTextWithLineFeeds.ToCharArray();
                    //var dst = currentTextWithLineFeeds.ToCharArray();

                    //for (int i = 0; i < src.Length; i++)
                    //{
                    //    var s = src[i];
                    //    var d = dst[i];

                    //    if (s != d)
                    //    {
                    //        code.NeedsRebuild = true;
                    //    }
                    //}

                    code.NeedsRebuild = true;
                    codeChanged = true;
                }

                code.Code = textEditor.AllText;


                //var inputFlags = ImGuiInputTextFlags.AllowTabInput;
                //if (ImGui.InputTextMultiline("##source",
                //        ref code.Code,
                //        20000,
                //        new Vector2(-1, -1),
                //        inputFlags))
                //{
                //    code.NeedsRebuild = true;
                //    codeChanged = true;
                //}

                ImGui.EndTabItem();
            }

            if (styleAttributesPushed > 0)
                ImGui.PopStyleColor(styleAttributesPushed);


            return codeChanged;
        }
    }
}