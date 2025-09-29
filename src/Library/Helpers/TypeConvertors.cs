
using Library.CodeVariable;

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
        };

        return table.GetValueOrDefault(input);
    }


}