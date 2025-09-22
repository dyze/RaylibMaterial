
using System.Numerics;
using Rectangle = Raylib_cs.Rectangle;

namespace Library.Helpers;

public static class TypeConvertors
{
    public static Raylib_cs.Color ToRayLibColor(System.Drawing.Color src)
    {
        return new Raylib_cs.Color(src.R, src.G, src.B, src.A);
    }

    public static Vector4 ColorToVec4(System.Drawing.Color src)
    {
        return new Vector4(src.R/255f, 
            src.G / 255f, 
            src.B / 255f,
            src.A / 255f);
    }

    public static System.Drawing.Color Vec4ToColor(Vector4 src)
    {
        return System.Drawing.Color.FromArgb((int)(src.W*255), 
            (int)(src.X * 255f), 
            (int)(src.Y * 255f), 
            (int)(src.Z * 255f));
    }

    public static Rectangle ToRayLibRectangle(System.Drawing.RectangleF src)
    {
        return new Rectangle(src.X, src.Y, src.Width, src.Height);
    }

    public static System.Drawing.RectangleF FromRayLibRectangle(Rectangle src)
    {
        return new System.Drawing.RectangleF(src.X, src.Y, src.Width, src.Height);
    }

    public static Type? StringToType(string input)
    {
        Dictionary<string, Type> table = new()
        {
            { "float", typeof(float) },
            { "vec2", typeof(Vector2) },
            { "vec3", typeof(Vector3) },
            { "vec4", typeof(Vector4) },
            { "int", typeof(int) },
            { "uint", typeof(uint) },
            { "sampler2D", typeof(string) },
        };

        return table.GetValueOrDefault(input);
    }


}