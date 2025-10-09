using System.Drawing;
using System.Numerics;
using Editor.Configuration;
using ImGuiNET;


namespace Editor.Ui.Windows;

internal class SettingsWindow(EditorConfiguration editorConfiguration)
{
    public Action? SavePressed;

    private bool _isVisible;

    private string? _outputDirectoryPath;
    private string? _resourceDirectoryPath;

    private string _errorMessage = "";
    private bool _selectFolderDialogIsOpen;

    private FileDialogInfo? _fileDialogInfo;

    private Func<string>? _actionOnSelect;

    public void Show()
    {
        _errorMessage = "";
        _outputDirectoryPath = editorConfiguration.OutputDirectoryPath;
        _resourceDirectoryPath = editorConfiguration.DataFileExplorerConfiguration.DataFolderPath;
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
                ImGui.InputText("Output directory", ref _outputDirectoryPath, 200);

                ImGui.SameLine();

                if (ImGui.Button("Select"))
                {
                    TriggerSelectFolder(_outputDirectoryPath ,
                        () => _outputDirectoryPath = _fileDialogInfo.ResultPath);
                }
            }
            ImGui.PopID();

            ImGui.PushID("Resource directory");
            {
                ImGui.InputText("Resource directory", ref _resourceDirectoryPath, 200);

                ImGui.SameLine();

                if (ImGui.Button("Select"))
                {
                    TriggerSelectFolder(_resourceDirectoryPath,
                        () => _resourceDirectoryPath = _fileDialogInfo.ResultPath);
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

            ImGui.TextColored(TypeConverters.ColorToVector4(Color.Red), _errorMessage);

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
        if (Directory.Exists(_outputDirectoryPath) == false)
        {
            _errorMessage = $"{_outputDirectoryPath} doesn't exist";
            return;
        }

        editorConfiguration.OutputDirectoryPath = _outputDirectoryPath;

        if (Directory.Exists(_resourceDirectoryPath) == false)
        {
            _errorMessage = $"{_resourceDirectoryPath} doesn't exist";
            return;
        }

        editorConfiguration.DataFileExplorerConfiguration.DataFolderPath = _resourceDirectoryPath;

        _errorMessage = "";
        SavePressed?.Invoke();
        ImGui.CloseCurrentPopup();
        _isVisible = false;
    }
}