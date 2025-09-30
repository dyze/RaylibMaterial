
using Library.CodeVariable;
using System.Numerics;
using Raylib_cs;

namespace Library.Helpers;

public static class TypeConvertors
{
    public static Type? StringToType(string input)
    {
        Dictionary<string, Type> table = new()
        {
            { "float", typeof(CodeVariableFloat) },
            //{ "vec2", typeof(Vector2) },
            { "vec3", typeof(CodeVariableVector3) },
            { "vec4", typeof(CodeVariableVector4) },
            { "mat4", typeof(CodeVariableMatrix4x4) },
            //{ "int", typeof(int) },
            //{ "uint", typeof(uint) },
            { "sampler2D", typeof(CodeVariableTexture) },
            { "Light", typeof(CodeVariableLight) },
        };

        return table.GetValueOrDefault(input);
    }

    public static Vector4 ColorToVector4(Color src)
    {
        return new Vector4((float)src.R / (float)byte.MaxValue, (float)src.G / (float)byte.MaxValue, (float)src.B / (float)byte.MaxValue, (float)src.A / (float)byte.MaxValue);
    }
}