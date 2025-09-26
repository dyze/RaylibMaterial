using ImGuiNET;

namespace Editor.Helpers;

static class ImGuiHelpers
{
    // Helper to display a little (?) mark which shows a tooltip when hovered.
    // In your own code you may want to display an actual icon if you are using a merged icon fonts (see docs/FONTS.md)
    public static void HelpMarker(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.BeginItemTooltip())
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()* 35.0f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    public static void RenderCheckedMenuItem(string caption,
        ref bool isChecked)
    {
        if (ImGui.MenuItem(caption, "", isChecked))
            isChecked = !isChecked;
    }
}