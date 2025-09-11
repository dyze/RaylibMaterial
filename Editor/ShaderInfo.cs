namespace Editor;

class ShaderInfo
{
    public string? VertexShaderFileName;
    public string? FragmentShaderFileName;

    public ShaderInfo(string? vertexShaderFileName, 
        string? fragmentShaderFileName)
    {
        VertexShaderFileName = vertexShaderFileName;
        FragmentShaderFileName = fragmentShaderFileName;
    }
}