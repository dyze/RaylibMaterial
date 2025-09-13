using Raylib_cs;

namespace Editor;

public class CodeVariable
{
    public ShaderUniformDataType Type;
    public string Name;

    public CodeVariable(ShaderUniformDataType type, string name)
    {
        Type = type;
        Name = name;
    }
}