using Editor.Messaging;
using Library;
using Library.Packaging;

namespace Editor.Tests;

[TestClass]
public class ShaderErrorParserTest
{
    [TestMethod]
    public void Parse()
    {
        Dictionary<FileId, ShaderCode> shaderCodes = [];

        shaderCodes.Add(new FileId(FileType.VertexShader, "file.vert"), new ShaderCode("dummy"));
        shaderCodes.Add(new FileId(FileType.FragmentShader, "file.frag"), new ShaderCode("dummy"));

        var shaderCode = shaderCodes.FirstOrDefault(s => s.Key.FileType == FileType.VertexShader);
        shaderCode.Value.Errors.Clear();

        var message = """SHADER: [ID 8] Failed to compile vertex shader code""";
        ShaderErrorParser.Parse(message, shaderCodes);

        Assert.AreEqual(8, shaderCode.Value.ShaderId);
        Assert.AreEqual(0, shaderCode.Value.Errors.Count);

        message = """
                  SHADER: [ID 8] Compile error: 0(22) : error C0000: syntax error, unexpected identifier, expecting ',' or ';' at token "Normal"
                  0(41) : error C1503: undefined variable "fragNormal"
                  0(43) : error C1503: undefined variable "fragNormal"
                  0(43) : error C1503: undefined variable "fragNormal"
                  0(45) : error C1503: undefined variable "fragNormal"
                  0(47) : error C1503: undefined variable "fragNormal"
                  """;
        ShaderErrorParser.Parse(message, shaderCodes);

        // 6 not 5 because there is a duplicate
        Assert.AreEqual(5, shaderCode.Value.Errors.Count);
        Assert.AreEqual("error C0000: syntax error, unexpected identifier, expecting ',' or ';' at token \"Normal\"", shaderCode.Value.Errors[22]);
        Assert.AreEqual("error C1503: undefined variable \"fragNormal\"", shaderCode.Value.Errors[47]);
    }
}   