namespace Editor;

class ShaderCode(string code)
{
    public bool Modified = false;
    public string Code = code;
    public bool IsValid { get; set; } = true;
}