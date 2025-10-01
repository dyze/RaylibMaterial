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
    private string ResourceDirectoryPath;

    private string ErrorMessage = "";
    private bool _selectFolderDialogIsOpen;

    private FileDialogInfo? _fileDialogInfo;

    private Func<string>? _actionOnSelect;

    public void Show()
    {
        ErrorMessage = "";
        OutputDirectoryPath = editorConfiguration.OutputDirectoryPath;
        ResourceDirectoryPath = editorConfiguration.DataFileExplorerConfiguration.DataFolderPath;
        _isVisible = true;
    }

    public void Render()
    {
        if (_isVisible)
            ImGui.OpenPopup("Settings");

        ImGui.SetNextWindowSize(new Vector2(600, 200), ImGuiCond.Always);
        if (ImGui.BeginPopupModal("Settings", ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoCollapse))
        {
            ImGui.PushID("Output directory");
            {
                ImGui.InputText("Output directory", ref OutputDirectoryPath, 200);

                ImGui.SameLine();

                if (ImGui.Button("Select"))
                {
                    TriggerSelectFolder(OutputDirectoryPath ,
                        () => OutputDirectoryPath = _fileDialogInfo.ResultPath);
                }
            }
            ImGui.PopID();

            ImGui.PushID("Resource directory");
            {
                ImGui.InputText("Resource directory", ref ResourceDirectoryPath, 200);

                ImGui.SameLine();

                if (ImGui.Button("Select"))
                {
                    TriggerSelectFolder(ResourceDirectoryPath,
                        () => ResourceDirectoryPath = _fileDialogInfo.ResultPath);
                }
            }
            ImGui.PopID();

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
                _actionOnSelect?.Invoke();
                _fileDialogInfo = null;
            }

            ImGui.EndPopup();
        }
    }

    private void TriggerSelectFolder(string startingPath, Func<string> actionOnSelect)
    {
        _fileDialogInfo = new()
        {
            Title = "Please select a folder",
            Type = ImGuiFileDialogType.SelectFolder,
            DirectoryPath = new DirectoryInfo(startingPath),
            DirectoryName = ""
        };

        _actionOnSelect = actionOnSelect;
        _selectFolderDialogIsOpen = true;
    }

    private void OnSave()
    {
        if (Directory.Exists(OutputDirectoryPath) == false)
        {
            ErrorMessage = $"{OutputDirectoryPath} doesn't exist";
            return;
        }

        editorConfiguration.OutputDirectoryPath = OutputDirectoryPath;

        if (Directory.Exists(ResourceDirectoryPath) == false)
        {
            ErrorMessage = $"{ResourceDirectoryPath} doesn't exist";
            return;
        }

        editorConfiguration.DataFileExplorerConfiguration.DataFolderPath = ResourceDirectoryPath;

        ErrorMessage = "";
        SavePressed?.Invoke();
        ImGui.CloseCurrentPopup();
        _isVisible = false;
    }
}