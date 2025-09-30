using System.Drawing;
using System.Numerics;
using Editor.Configuration;
using ImGuiNET;


namespace Editor.Windows;

internal class SettingsWindow(EditorConfiguration editorConfiguration)
{
    public Action? SavePressed;

    private bool _isVisible;

    private string OutputDirectoryPath;

    private string ErrorMessage = "";
    private bool _selectFolderDialogIsOpen;

    private FileDialogInfo? _fileDialogInfo;

    public void Show()
    {
        ErrorMessage = "";
        OutputDirectoryPath = editorConfiguration.OutputDirectoryPath;
        _isVisible = true;
    }

    public void Render()
    {
        if (_isVisible)
            ImGui.OpenPopup("Settings");

        ImGui.SetNextWindowSize(new Vector2(600, 200), ImGuiCond.Always);
        if (ImGui.BeginPopupModal("Settings", ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoCollapse))
        {
            if (ImGui.InputText("Output directory", ref OutputDirectoryPath, 200))
            {
            }

            ImGui.SameLine();

            if (ImGui.Button("Select"))
            {
                _fileDialogInfo = new()
                {
                    Title = "Please select a folder",
                    Type = ImGuiFileDialogType.SelectFolder,
                    DirectoryPath = new DirectoryInfo(Directory.GetCurrentDirectory()),
                    DirectoryName = ""
                };
                _selectFolderDialogIsOpen = true;
            }

            ImGui.Separator();

            if (ImGui.Button("Cancel"))
            {
                _isVisible = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ColoredButton.Run(Color.Green, "Save"))
            {
                OnSave();
            }

            ImGui.TextColored(TypeConverters.ColorToVector4(Color.Red), ErrorMessage);

            if (FileDialog.Run(ref _selectFolderDialogIsOpen, _fileDialogInfo))
            {
                OutputDirectoryPath = _fileDialogInfo.ResultPath;
                _fileDialogInfo = null;
            }

            ImGui.EndPopup();
        }



    }

    private void OnSave()
    {
        if (Directory.Exists(OutputDirectoryPath) == false)
        {
            ErrorMessage = $"{OutputDirectoryPath} doesn't exist";
            return;
        }

        editorConfiguration.OutputDirectoryPath = OutputDirectoryPath;

        ErrorMessage = "";
        SavePressed?.Invoke();
        ImGui.CloseCurrentPopup();
        _isVisible = false;
    }
}