

using Editor;
using Raylib_cs;

namespace Library.Tests;

[TestClass]
public sealed class ShaderCodeTest
{
    [TestMethod]
    public void Parse()
    {
        var code = @"#version 330

                        // Input vertex attributes (from vertex shader)
                        in vec2 fragTexCoord;
                        in vec4 fragColor;

                        // Input uniform values
                        uniform sampler2D texture0;
                        uniform vec4 colDiffuse;

                        // Output fragment color
                        out vec4 finalColor;

                        // NOTE: Add here your custom variables

                        void main()
                        {
                            // Texel color fetching from texture sampler
                            vec4 texelColor = texture(texture0, fragTexCoord);

                            // NOTE: Implement here your fragment shader code

                            finalColor = texelColor*colDiffuse;
                        }";

        var shaderCode = new ShaderCode(code);
        Assert.IsNotNull(shaderCode);

       shaderCode.ParseVariables();
       var variables = shaderCode.Variables;

       Assert.AreEqual(variables.Count, 2);

        Assert.AreEqual(variables[0].Type, ShaderUniformDataType.Sampler2D);
        Assert.AreEqual(variables[0].Name, "texture0");
    }
}
