using System.Drawing;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;
using NLog;
using Raylib_cs;

namespace Editor.Windows;

class ImageWindow
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Texture2D _texture;

    public Action<ImageWindow>? CloseRequest;
    private readonly string _fileName;

    public ImageWindow(string fileName,
        byte[] imageData)
    {
        _fileName = fileName;

        _texture = new Texture2D();

        var extension = Path.GetExtension(fileName);
        if (extension == null)
            throw new NullReferenceException($"No file extension found in {fileName}");

        var image = Raylib.LoadImageFromMemory(extension, imageData); // ignore period
        if (Raylib.IsImageValid(image) == false)
        {
            Logger.Debug($"image {fileName} is not valid");
            return;
        }

        _texture = Raylib.LoadTextureFromImage(image);

        Raylib.UnloadImage(image);
    }

    public void Render()
    {
        var open = true;
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse; // | ImGuiWindowFlags.AlwaysAutoResize;

        // Fit and center window in main window
        var mainWindowSize = new Size(Raylib.GetScreenWidth(),
            Raylib.GetScreenHeight());

        var windowSize = mainWindowSize * 0.9f;
        var newSize = new Vector2(_texture.Width, _texture.Height);
        {
            var ratio = _texture.Width / _texture.Height;

            if (_texture.Width > windowSize.Width)
            {
                newSize.X = windowSize.Width;
                newSize.Y = newSize.X * ratio;
            }

            if (newSize.Y > windowSize.Height)
            {
                newSize.Y = windowSize.Height;
                newSize.X = newSize.Y / ratio;
            }
        }

        ImGui.SetNextWindowPos(new Vector2((mainWindowSize.Width - newSize.X) / 2,
                (mainWindowSize.Height - newSize.Y) / 2),
            ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(newSize, 
            ImGuiCond.Appearing);

        if (ImGui.Begin(_fileName, ref open, flags))
        {
            ImGui.BeginDisabled();
            var size = new Vector2(_texture.Width, _texture.Height);
            ImGui.InputFloat2("Size", ref size);

            ImGui.LabelText("Format", _texture.Format.ToString());
            ImGui.EndDisabled();

            ImGui.BeginChild("## image");

            var available = ImGui.GetContentRegionAvail();

            // Preserve image ratio
            var ratioX = (double)available.X / (double)_texture.Width;
            var ratioY = (double)available.Y / (double)_texture.Height;

            var ratio = ratioX < ratioY ? ratioX : ratioY;

            var newWidth = Convert.ToInt32(_texture.Width * ratio);
            var newHeight = Convert.ToInt32(_texture.Height * ratio);

            var offsetX = (available.X - newWidth) / 2;
            if (offsetX > 10) // 10 to avoid win size flickering
                ImGui.Indent(offsetX);

            var offsetY = (available.Y - newHeight) / 2;
            if (offsetY > 10) // 10 to avoid win size flickering
                ImGui.Dummy(new Vector2(0.0f, offsetY));

            rlImGui.ImageSize(_texture, newWidth, newHeight);

            ImGui.EndChild();
        }

        if (open == false)
            CloseRequest?.Invoke(this);

        ImGui.End();
    }
}