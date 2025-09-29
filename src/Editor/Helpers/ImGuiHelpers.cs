using ImGuiNET;

namespace Editor.Helpers;

static class ImGuiHelpers
{
    public static void RenderCheckedMenuItem(string caption,
        ref bool isChecked)
    {
        if (ImGui.MenuItem(caption, "", isChecked))
            isChecked = !isChecked;
    }
}