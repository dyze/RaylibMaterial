using System.Drawing;
using System.Numerics;
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;

namespace Editor;

public static class TypeConvertors
{
    public static Color ToRayLibColor(System.Drawing.Color src)
    {
        return new Color(src.R, src.G, src.B, src.A);
    }

    public static Vector4 ToVector4(System.Drawing.Color src)
    {
        return new Vector4(src.R/255f, 
            src.G / 255f, 
            src.B / 255f,
            src.A / 255f);
    }

    public static System.Drawing.Color FromVector4(Vector4 src)
    {
        return System.Drawing.Color.FromArgb((int)(src.W*255), 
            (int)(src.X * 255f), 
            (int)(src.Y * 255f), 
            (int)(src.Z * 255f));
    }

    public static Rectangle ToRayLibRectangle(RectangleF src)
    {
        return new Rectangle(src.X, src.Y, src.Width, src.Height);
    }

    public static RectangleF FromRayLibRectangle(Rectangle src)
    {
        return new RectangleF(src.X, src.Y, src.Width, src.Height);
    }


}