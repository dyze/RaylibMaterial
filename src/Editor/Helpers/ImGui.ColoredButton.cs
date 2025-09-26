using System.Numerics;
using ImGuiNET;

namespace Editor.Helpers;

public static class ColoredButton
{
    public static bool Render(Vector4 color, string text)
    {
        ImGui.ColorConvertRGBtoHSV(color.X, color.Y, color.Z, out var h, out var s, out var v);

        // brighter
        ImGui.ColorConvertHSVtoRGB(h, s, v * 1.4f, out var r, out var g, out var b);
        var buttonHoveredColor = new Vector4(r, g, b, 1f);

        // darker
        ImGui.ColorConvertHSVtoRGB(h, s, v * 0.8f, out r, out g, out b);
        var buttonActiveColor = new Vector4(r, g, b, 1f);

        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonActiveColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonHoveredColor);

        var result = ImGui.Button(text);

        ImGui.PopStyleColor(3);
        return result;
    }
}