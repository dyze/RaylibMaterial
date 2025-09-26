using System.Drawing;
using Library.Helpers;

namespace ImGuiNET
{

    public class ImGuiMessageDialog
    {
        public enum ButtonId
        {
            Ok = 0,
            Cancel,
            Yes,
            No
        }

        public class ButtonConfiguration(ButtonId id, 
            string caption, 
            Action<ButtonConfiguration>? onPressed = null,
            Color? color = null)
        {
            public ButtonId Id { get; } = id;
            public string Caption { get; } = caption;
            public Action<ButtonConfiguration>? OnPressed { get; } = onPressed;
            public Color? Color { get; } = color;
        }

        public class Configuration(string caption, string detailedText, List<ButtonConfiguration> buttons)
        {
            public string Caption { get; } = caption;
            public string DetailedText { get; } = detailedText;
            public List<ButtonConfiguration> Buttons { get; } = buttons;
        }

        public static ButtonConfiguration? Run(Configuration? configuration)
        {
            if (configuration == null)
                return null;

            ButtonConfiguration? buttonPressed = null;

            ImGui.PushID(configuration.Caption);
            //ImGui.SetNextWindowSize(new Vector2(600, 300.0f), ImGuiCond.FirstUseEver);

            ImGui.OpenPopup(configuration.Caption);

            var open = true;

            if (ImGui.BeginPopupModal(configuration.Caption,
                    ref open, 
                    ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(configuration.DetailedText);

                ImGui.Separator();

                var first = false;
                foreach (var button in configuration.Buttons)
                {
                    if (first == false)
                        ImGui.SameLine();

                    if(button.Color != null)
                        ImGui.PushStyleColor(ImGuiCol.Button, TypeConvertors.ColorToVector4(button.Color.Value));

                    if (ImGui.Button(button.Caption))
                    {
                        buttonPressed = button;
                    }

                    if (button.Color != null)
                        ImGui.PopStyleColor();

                    first = false;
                }

                ImGui.End();
            }


            ImGui.PopID();

            return buttonPressed;
        }
    }
}
