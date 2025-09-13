using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using Color = System.Drawing.Color;


namespace Editor;

internal class MessageWindow
{
    private bool _autoScroll = true;

    private static readonly Dictionary<LogLevel, Color> LogLevelColors = new()
    {
        { LogLevel.Debug, Color.LightBlue },
        { LogLevel.Trace, Color.LightBlue },
        { LogLevel.Info, Color.White },
        { LogLevel.Warning, Color.Yellow },
        { LogLevel.Error, Color.Orange },
        { LogLevel.Fatal, Color.Red },
    };

    public void Render(MessageQueue queue, ref bool isVisible)
    {
        if (isVisible == false)
            return;

        var size = new Vector2(1000, 200);
        var position = new Vector2(0, Raylib.GetScreenHeight() - size.Y);

        ImGui.SetNextWindowPos(position, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Messages", ref isVisible))
        {
            // Options menu
            if (ImGui.BeginPopup("Options"))
            {
                ImGui.Checkbox("Auto-scroll", ref _autoScroll);
                ImGui.EndPopup();
            }

            // Main window
            if (ImGui.Button("Options"))
                ImGui.OpenPopup("Options");
            ImGui.SameLine();
            var clear = ImGui.Button("Clear");
            ImGui.SameLine();
            var copy = ImGui.Button("Copy");
            ImGui.Separator();

            if (ImGui.BeginChild("scrolling", new Vector2(0, 0), ImGuiChildFlags.None,
                    ImGuiWindowFlags.HorizontalScrollbar))
            {
                if (clear)
                    queue.Clear();

                if (copy)
                    ImGui.LogToClipboard();

                var messages = queue.GetMessages();

                foreach (var message in messages)
                {
                    var color = LogLevelColors[message.logLevel];
                    ImGui.TextColored(TypeConvertors.ToVector4(color),
                        message.text);
                }

                if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1.0f);

            }
            ImGui.EndChild();
        }

        ImGui.End();
    }
}
