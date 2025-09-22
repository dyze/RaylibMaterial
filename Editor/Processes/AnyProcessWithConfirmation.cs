using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Library.Helpers;


namespace Editor.Processes;

internal class AnyProcessWithConfirmation : EditorProcess
{
    private readonly string _prompt;
    private readonly Action _actionToPerform;

    public AnyProcessWithConfirmation(string prompt, 
        Action actionToPerform)
    {
        _actionToPerform = actionToPerform;
        _prompt = prompt;
    }

    public override bool IsValid()
    {
        return true;
    }

    public override bool Render()
    {
        var closed = false;

        ImGui.OpenPopup("please confirm");

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("please confirm", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(_prompt);

            if (ErrorMessage.Length > 0)
            {
                ImGui.Separator();
                ImGui.PushStyleColor(ImGuiCol.Text, TypeConvertors.ToVector4(Color.Orange));
                ImGui.TextWrapped(ErrorMessage);
                ImGui.PopStyleColor();
                ImGui.Separator();
            }

            ImGui.BeginDisabled(IsValid() == false);
            if (ImGui.Button("Yes"))
            {
                try
                {
                    _actionToPerform();

                    closed = true;
                    ImGui.CloseCurrentPopup();
                }
                catch (Exception e)
                {
                    ErrorMessage = e.Message;
                }
            }

            ImGui.EndDisabled();

            ImGui.SameLine();

            if (ImGui.Button("No"))
            {
                closed = true;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return closed;
    }
}