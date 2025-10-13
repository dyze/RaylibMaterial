using Library.CodeVariable;

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

        Assert.AreEqual(variables["texture0"].GetType(), typeof(CodeVariableTexture));
        Assert.AreEqual(variables["colDiffuse"].GetType(), typeof(CodeVariableInternal));
    }

    [TestMethod]
    public void ParseAllTypes()
    {
        var code = @"#version 330

                        // Input vertex attributes (from vertex shader)
                        in vec2 fragTexCoord;
                        in vec4 fragColor;

                        // Input uniform values
                        uniform sampler2D vTexture;

                        uniform float vFloat;
                        uniform vec2 vVec2;
                        uniform vec3 vVec3;
                        uniform vec4 vVec4;

                        uniform uint Uint;
                        uniform uvec2 uVec2;
                        uniform uvec3 uVec3;
                        uniform uvec4 uVec4;

                        uniform int iInt;
                        uniform ivec2 iVec2;
                        uniform ivec3 iVec3;
                        uniform ivec4 iVec4;

                        uniform Light lights[2];

                        uniform mat4 Matrix4x4;

                        uniform myType vMyType;


                        // Output fragment color
                        out vec4 finalColor;


                        void main()
                        {

                        }";

        var shaderCode = new ShaderCode(code);
        Assert.IsNotNull(shaderCode);

        shaderCode.ParseVariables();
        var variables = shaderCode.Variables;

        Assert.AreEqual(variables.Count, 16);

        Assert.AreEqual(variables["vTexture"].GetType(), typeof(CodeVariableTexture));
        Assert.AreEqual(variables["lights"].GetType(), typeof(CodeVariableInternal));
        Assert.AreEqual(variables["vVec4"].GetType(), typeof(CodeVariableVector4));
        Assert.AreEqual(variables["uVec4"].GetType(), typeof(CodeVariableUVector4));
        Assert.AreEqual(variables["iVec4"].GetType(), typeof(CodeVariableIVector4));
        Assert.AreEqual(variables["Matrix4x4"].GetType(), typeof(CodeVariableMatrix4x4));
        Assert.AreEqual(variables["lights"].GetType(), typeof(CodeVariableInternal));
        Assert.AreEqual(variables["vMyType"].GetType(), typeof(CodeVariableUnsupported));
    }
}