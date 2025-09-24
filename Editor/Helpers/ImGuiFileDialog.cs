/*
    Copyright 2020 Limeoats
    Original project: https://github.com/Limeoats/L2DFileDialog
    Changes by Vladimir Sigalkin https://github.com/Iam1337/ImGui-FileDialog
    Ported to C# by W.M.R Jap-A-Joe https://github.com/japajoe
*/

using System.Numerics;
using ImGuiNET;

namespace Editor.Helpers
{
    public enum ImGuiFileDialogType
    {
        OpenFile,
        SaveFile
    }

    public enum ImGuiFileDialogSortOrder
    {
        Up,
        Down,
        None
    }

    public class ImFileDialogInfo
    {
        public string Title;
        public ImGuiFileDialogType Type;

        public string FileName;
        public DirectoryInfo DirectoryPath;
        public string ResultPath;

        public bool RefreshInfo;
        public ulong CurrentIndex;
        public List<FileInfo> CurrentFiles;
        public List<DirectoryInfo> CurrentDirectories;

        public List<Tuple<string, string>> Extensions = [new("*.*", "All files")];
        public int CurrentExtensionIndex = 0;
        public Tuple<string, string> CurrentExtension => Extensions[CurrentExtensionIndex];

        public ImFileDialogInfo()
        {
            CurrentFiles = new List<FileInfo>();
            CurrentDirectories = new List<DirectoryInfo>();
        }
    }

    public class ImGuiFileDialog
    {
        private static void RefreshInfo(ImFileDialogInfo dialogInfo)
        {
            dialogInfo.RefreshInfo = false;
            dialogInfo.CurrentIndex = 0;
            dialogInfo.CurrentFiles.Clear();
            dialogInfo.CurrentDirectories.Clear();

            var directory = new DirectoryInfo(dialogInfo.DirectoryPath.FullName);

            dialogInfo.CurrentDirectories = directory.GetDirectories().ToList();
            dialogInfo.CurrentFiles = directory.GetFiles(dialogInfo.CurrentExtension.Item1).ToList();
        }

        private static float _initialSpacingColumn0 = 230.0f;
        private static float _initialSpacingColumn1 = 80.0f;
        private static float _initialSpacingColumn2 = 90.0f;
        private static ImGuiFileDialogSortOrder _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
        private static ImGuiFileDialogSortOrder _sizeSortOrder = ImGuiFileDialogSortOrder.None;
        private static ImGuiFileDialogSortOrder _dateSortOrder = ImGuiFileDialogSortOrder.None;
        private static ImGuiFileDialogSortOrder _typeSortOrder = ImGuiFileDialogSortOrder.None;

        private static ImGuiFileDialogSortOrder _fileNameSortOrderCopy = ImGuiFileDialogSortOrder.None;
        private static ImGuiFileDialogSortOrder _sizeSortOrderCopy = ImGuiFileDialogSortOrder.None;
        private static ImGuiFileDialogSortOrder _dateSortOrderCopy = ImGuiFileDialogSortOrder.None;
        private static ImGuiFileDialogSortOrder _typeSortOrderCopy = ImGuiFileDialogSortOrder.None;

        private static void Sort(ImFileDialogInfo dialogInfo, bool forceSort = false)
        {
            //var directories = dialogInfo.currentDirectories;
            //var files = dialogInfo.currentFiles;            
            var sort = false;

            if (_fileNameSortOrderCopy != _fileNameSortOrder)
            {
                _fileNameSortOrderCopy = _fileNameSortOrder;
                sort = true;
            }

            if (_sizeSortOrderCopy != _sizeSortOrder)
            {
                _sizeSortOrderCopy = _sizeSortOrder;
                sort = true;
            }

            if (_dateSortOrderCopy != _dateSortOrder)
            {
                _dateSortOrderCopy = _dateSortOrder;
                sort = true;
            }

            if (_typeSortOrderCopy != _typeSortOrder)
            {
                _typeSortOrderCopy = _typeSortOrder;
                sort = true;
            }

            if (!sort && !forceSort)
                return;

            // Sort directories

            if (_fileNameSortOrder != ImGuiFileDialogSortOrder.None || _sizeSortOrder != ImGuiFileDialogSortOrder.None || _typeSortOrder != ImGuiFileDialogSortOrder.None)
            {
                if (_fileNameSortOrder == ImGuiFileDialogSortOrder.Down)
                {
                    dialogInfo.CurrentDirectories = dialogInfo.CurrentDirectories.OrderBy(i => i.Name).ToList();
                }
                else
                {
                    dialogInfo.CurrentDirectories = dialogInfo.CurrentDirectories.OrderBy(i => i.Name).ToList();
                    dialogInfo.CurrentDirectories.Reverse();
                }
            }
            else if (_dateSortOrder != ImGuiFileDialogSortOrder.None)
            {
                if (_dateSortOrder == ImGuiFileDialogSortOrder.Down)
                {
                    dialogInfo.CurrentDirectories.Sort((a, b) => a.LastWriteTime > b.LastWriteTime ? 1 : 0);
                }
                else
                {
                    dialogInfo.CurrentDirectories.Sort((a, b) => a.LastWriteTime < b.LastWriteTime ? 1 : 0);
                }
            }

            // Sort files
            if (_fileNameSortOrder != ImGuiFileDialogSortOrder.None)
            {
                if (_fileNameSortOrder == ImGuiFileDialogSortOrder.Down)
                {
                    dialogInfo.CurrentFiles = dialogInfo.CurrentFiles.OrderBy(i => i.Name).ToList();
                }
                else
                {
                    dialogInfo.CurrentFiles = dialogInfo.CurrentFiles.OrderBy(i => i.Name).ToList();
                    dialogInfo.CurrentFiles.Reverse();
                }
            }
            else if (_sizeSortOrder != ImGuiFileDialogSortOrder.None)
            {
                if (_sizeSortOrder == ImGuiFileDialogSortOrder.Down)
                {
                    dialogInfo.CurrentFiles.Sort((a, b) => a.Length > b.Length ? 1 : 0);
                }
                else
                {
                    dialogInfo.CurrentFiles.Sort((a, b) => a.Length < b.Length ? 1 : 0);
                }
            }
            else if (_typeSortOrder != ImGuiFileDialogSortOrder.None)
            {
                if (_typeSortOrder == ImGuiFileDialogSortOrder.Down)
                {
                    dialogInfo.CurrentFiles = dialogInfo.CurrentFiles.OrderBy(i => i.Extension).ToList();
                }
                else
                {
                    dialogInfo.CurrentFiles = dialogInfo.CurrentFiles.OrderBy(i => i.Extension).ToList();
                    dialogInfo.CurrentFiles.Reverse();
                }
            }
            else if (_dateSortOrder != ImGuiFileDialogSortOrder.None)
            {
                if (_dateSortOrder == ImGuiFileDialogSortOrder.Down)
                {
                    dialogInfo.CurrentFiles.Sort((a, b) => a.LastWriteTime > b.LastWriteTime ? 1 : 0);
                }
                else
                {
                    dialogInfo.CurrentFiles.Sort((a, b) => a.LastWriteTime < b.LastWriteTime ? 1 : 0);
                }
            }
        }

        public static bool FileDialog(ref bool open,
            ImFileDialogInfo dialogInfo)
        {
            if (!open)
                return false;

            if (dialogInfo == null)
                return false;

            var complete = false;

            ImGui.PushID(dialogInfo.GetHashCode());
            ImGui.SetNextWindowSize(new Vector2(740.0f, 410.0f), ImGuiCond.FirstUseEver);

            if (ImGui.Begin(dialogInfo.Title, ref open, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {
                if (dialogInfo.CurrentFiles.Count == 0 && dialogInfo.CurrentDirectories.Count == 0 || dialogInfo.RefreshInfo)
                    RefreshInfo(dialogInfo);

                // Draw path
                ImGui.Text("Path: " + dialogInfo.DirectoryPath);

                var contentRegionWidth = ImGui.GetContentRegionAvail().X;

                ImGui.BeginChild("##browser", new Vector2(contentRegionWidth, 300), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
                ImGui.Columns(4);

                // Columns size
                if (_initialSpacingColumn0 > 0)
                {
                    ImGui.SetColumnWidth(0, _initialSpacingColumn0);
                    _initialSpacingColumn0 = 0.0f;
                }
                if (_initialSpacingColumn1 > 0)
                {
                    ImGui.SetColumnWidth(1, _initialSpacingColumn1);
                    _initialSpacingColumn1 = 0.0f;
                }
                if (_initialSpacingColumn2 > 0)
                {
                    ImGui.SetColumnWidth(2, _initialSpacingColumn2);
                    _initialSpacingColumn2 = 0.0f;
                }

                // File Columns
                if (ImGui.Selectable("Name"))
                {
                    _sizeSortOrder = ImGuiFileDialogSortOrder.None;
                    _dateSortOrder = ImGuiFileDialogSortOrder.None;
                    _typeSortOrder = ImGuiFileDialogSortOrder.None;
                    _fileNameSortOrder = _fileNameSortOrder == ImGuiFileDialogSortOrder.Down ? ImGuiFileDialogSortOrder.Up : ImGuiFileDialogSortOrder.Down;
                    _fileNameSortOrderCopy = _fileNameSortOrder;
                    Sort(dialogInfo, true);
                }
                ImGui.NextColumn();
                if (ImGui.Selectable("Size"))
                {
                    _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
                    _dateSortOrder = ImGuiFileDialogSortOrder.None;
                    _typeSortOrder = ImGuiFileDialogSortOrder.None;
                    _sizeSortOrder = _sizeSortOrder == ImGuiFileDialogSortOrder.Down ? ImGuiFileDialogSortOrder.Up : ImGuiFileDialogSortOrder.Down;
                    _sizeSortOrderCopy = _sizeSortOrder;
                    Sort(dialogInfo, true);
                }
                ImGui.NextColumn();
                if (ImGui.Selectable("Type"))
                {
                    _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
                    _dateSortOrder = ImGuiFileDialogSortOrder.None;
                    _sizeSortOrder = ImGuiFileDialogSortOrder.None;
                    _typeSortOrder = _typeSortOrder == ImGuiFileDialogSortOrder.Down ? ImGuiFileDialogSortOrder.Up : ImGuiFileDialogSortOrder.Down;
                    _typeSortOrderCopy = _typeSortOrder;
                    Sort(dialogInfo, true);
                }
                ImGui.NextColumn();
                if (ImGui.Selectable("Date"))
                {
                    _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
                    _sizeSortOrder = ImGuiFileDialogSortOrder.None;
                    _typeSortOrder = ImGuiFileDialogSortOrder.None;
                    _dateSortOrder = _dateSortOrder == ImGuiFileDialogSortOrder.Down ? ImGuiFileDialogSortOrder.Up : ImGuiFileDialogSortOrder.Down;
                    _dateSortOrderCopy = _dateSortOrder;
                    Sort(dialogInfo, true);
                }
                ImGui.NextColumn();

                // File Separator
                ImGui.Separator();

                // Sort directories
                var directories = dialogInfo.CurrentDirectories;
                var files = dialogInfo.CurrentFiles;

                Sort(dialogInfo);

                UInt64 index = 0;

                // Draw parent
                if (dialogInfo.DirectoryPath.Parent != null)
                {
                    contentRegionWidth = ImGui.GetContentRegionAvail().X;

                    if (ImGui.Selectable("..",
                            dialogInfo.CurrentIndex == index,
                            ImGuiSelectableFlags.AllowDoubleClick,
                            new Vector2(contentRegionWidth, 0)))
                    {
                        dialogInfo.CurrentIndex = index;

                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            dialogInfo.DirectoryPath = dialogInfo.DirectoryPath.Parent;
                            dialogInfo.RefreshInfo = true;
                            Sort(dialogInfo, true);
                        }
                    }
                    ImGui.NextColumn();
                    ImGui.TextUnformatted("-");
                    ImGui.NextColumn();
                    ImGui.TextUnformatted("<parent>");
                    ImGui.NextColumn();
                    ImGui.TextUnformatted("-");
                    ImGui.NextColumn();

                    index++;
                }

                // Draw directories
                for (var i = 0; i < directories.Count; ++i)
                {
                    var directoryEntry = dialogInfo.CurrentDirectories[i];
                    var directoryPath = directoryEntry;
                    var directoryName = directoryEntry.Name;

                    contentRegionWidth = ImGui.GetContentRegionAvail().X;

                    if (ImGui.Selectable(directoryName, dialogInfo.CurrentIndex == index, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(contentRegionWidth, 0)))
                    {
                        dialogInfo.CurrentIndex = index;

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            dialogInfo.DirectoryPath = directoryPath;
                            dialogInfo.RefreshInfo = true;
                            Sort(dialogInfo, true);
                        }
                    }

                    ImGui.NextColumn();
                    ImGui.TextUnformatted("-");
                    ImGui.NextColumn();
                    ImGui.TextUnformatted("<directory>");
                    ImGui.NextColumn();

                    var lastWriteTime = directoryEntry.LastWriteTime;
                    ImGui.TextUnformatted(lastWriteTime.ToString());
                    ImGui.NextColumn();

                    index++;
                }

                // Draw files
                for (var i = 0; i < files.Count; ++i)
                {
                    var fileEntry = dialogInfo.CurrentFiles[i];
                    var filePath = fileEntry.FullName;
                    var fileName = fileEntry.Name;

                    contentRegionWidth = ImGui.GetContentRegionAvail().X;

                    if (ImGui.Selectable(fileName, dialogInfo.CurrentIndex == index, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(contentRegionWidth, 0)))
                    {
                        dialogInfo.CurrentIndex = index;
                        dialogInfo.FileName = fileName;

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            complete = OnOkPressed(ref open, dialogInfo, complete);
                        }
                    }

                    ImGui.NextColumn();
                    ImGui.TextUnformatted(fileEntry.Length.ToString());
                    ImGui.NextColumn();
                    ImGui.TextUnformatted(Path.GetExtension(filePath));
                    ImGui.NextColumn();

                    var lastWriteTime = fileEntry.LastWriteTime;
                    ImGui.TextUnformatted(lastWriteTime.ToString());
                    ImGui.NextColumn();

                    index++;
                }
                ImGui.EndChild();

                // Draw filename
                var fileNameBufferSize = 200;

                var fileNameStr = dialogInfo.FileName;
                var fileNameSize = fileNameStr.Length;

                if (fileNameSize >= fileNameBufferSize)
                    fileNameSize = fileNameBufferSize - 1;

                var fileNameBuffer = fileNameStr.Substring(0, fileNameSize);

                contentRegionWidth = ImGui.GetContentRegionAvail().X;

                ImGui.PushID("filename");
                ImGui.PushItemWidth(contentRegionWidth * 0.7f);
                if (ImGui.InputTextWithHint("", "select a file first", ref fileNameBuffer, (uint)fileNameBufferSize))
                {
                    dialogInfo.FileName = fileNameBuffer;
                    dialogInfo.CurrentIndex = 0;
                }
                ImGui.PopID();

                ImGui.PushID("type");
                ImGui.SameLine();
                ImGui.PushItemWidth(contentRegionWidth * 0.3f);
                if (ImGui.BeginCombo("Type", dialogInfo.CurrentExtension.Item1))
                {
                    foreach (var (extension, description) in dialogInfo.Extensions)
                    {
                        var selected = extension == dialogInfo.CurrentExtension.Item1;
                        ImGui.PushItemWidth(contentRegionWidth * 0.3f);
                        if (ImGui.Selectable(extension, selected))
                        {
                            dialogInfo.CurrentExtensionIndex =
                               dialogInfo.Extensions.FindIndex(e => e.Item1 == extension);
                            dialogInfo.RefreshInfo = true;
                        }
                        if (selected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }
                ImGui.PopID();

                if (ImGui.Button("Cancel"))
                {
                    _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
                    _sizeSortOrder = ImGuiFileDialogSortOrder.None;
                    _typeSortOrder = ImGuiFileDialogSortOrder.None;
                    _dateSortOrder = ImGuiFileDialogSortOrder.None;

                    dialogInfo.RefreshInfo = false;
                    dialogInfo.CurrentIndex = 0;
                    dialogInfo.CurrentFiles.Clear();
                    dialogInfo.CurrentDirectories.Clear();

                    open = false;
                }

                ImGui.SameLine();

                if (dialogInfo.Type == ImGuiFileDialogType.OpenFile)
                {
                    if (ImGui.Button("Open"))
                    {
                        complete = OnOkPressed(ref open, dialogInfo, complete);
                    }
                }
                else if (dialogInfo.Type == ImGuiFileDialogType.SaveFile)
                {
                    if (ImGui.Button("Save"))
                    {
                        dialogInfo.ResultPath = Path.Combine(dialogInfo.DirectoryPath.FullName, dialogInfo.FileName);

                        if (Directory.Exists(dialogInfo.DirectoryPath.FullName))
                        {
                            _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
                            _sizeSortOrder = ImGuiFileDialogSortOrder.None;
                            _typeSortOrder = ImGuiFileDialogSortOrder.None;
                            _dateSortOrder = ImGuiFileDialogSortOrder.None;

                            dialogInfo.RefreshInfo = false;
                            dialogInfo.CurrentIndex = 0;
                            dialogInfo.CurrentFiles.Clear();
                            dialogInfo.CurrentDirectories.Clear();

                            complete = true;
                            open = false;
                        }
                    }
                }
            }

            ImGui.End();
            ImGui.PopID();

            return complete;
        }

        private static bool OnOkPressed(ref bool open, ImFileDialogInfo dialogInfo, bool complete)
        {
            dialogInfo.ResultPath = Path.Combine(dialogInfo.DirectoryPath.FullName, dialogInfo.FileName);

            if (File.Exists(dialogInfo.ResultPath))
            {
                _fileNameSortOrder = ImGuiFileDialogSortOrder.None;
                _sizeSortOrder = ImGuiFileDialogSortOrder.None;
                _typeSortOrder = ImGuiFileDialogSortOrder.None;
                _dateSortOrder = ImGuiFileDialogSortOrder.None;

                dialogInfo.RefreshInfo = false;
                dialogInfo.CurrentIndex = 0;
                dialogInfo.CurrentFiles.Clear();
                dialogInfo.CurrentDirectories.Clear();

                complete = true;
                open = false;
            }

            return complete;
        }
    }
}
