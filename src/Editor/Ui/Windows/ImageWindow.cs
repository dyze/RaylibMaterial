using System.Drawing;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;
using NLog;
using Raylib_cs;

namespace Editor.Ui.Windows;

class ImageWindow
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Texture2D? _texture;

    public Action<ImageWindow>? CloseRequest;
    private readonly string _fileName;

    public ImageWindow(string fileName,
        byte[] imageData)
    {
        _fileName = fileName;
        
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

        var textureSize = new Size(200, 100);
        var textureFormat = "invalid image";
        if (_texture != null)
        {
            textureSize = new Size(_texture.Value.Width, _texture.Value.Height);
            textureFormat = _texture.Value.Format.ToString();
        }

        var windowSize = mainWindowSize * 0.9f;
        var newSize = new Vector2(textureSize.Width, textureSize.Height);
        {
            var ratio = textureSize.Width / textureSize.Height;

            if (textureSize.Width > windowSize.Width)
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
            var size = new Vector2(textureSize.Width, textureSize.Height);
            ImGui.InputFloat2("Size", ref size);

            ImGui.LabelText("Format", textureFormat);
            ImGui.EndDisabled();

            ImGui.BeginChild("## image");

            var available = ImGui.GetContentRegionAvail();

            // Preserve image ratio
            var ratioX = available.X / (double)textureSize.Width;
            var ratioY = available.Y / (double)textureSize.Height;

            var ratio = ratioX < ratioY ? ratioX : ratioY;

            var newWidth = Convert.ToInt32(textureSize.Width * ratio);
            var newHeight = Convert.ToInt32(textureSize.Height * ratio);

            var offsetX = (available.X - newWidth) / 2;
            if (offsetX > 10) // 10 to avoid win size flickering
                ImGui.Indent(offsetX);

            var offsetY = (available.Y - newHeight) / 2;
            if (offsetY > 10) // 10 to avoid win size flickering
                ImGui.Dummy(new Vector2(0.0f, offsetY));

            if(_texture.HasValue)
                rlImGui.ImageSize(_texture.Value, newWidth, newHeight);

            ImGui.EndChild();
        }

        if (open == false)
            CloseRequest?.Invoke(this);

        ImGui.End();
    }
}