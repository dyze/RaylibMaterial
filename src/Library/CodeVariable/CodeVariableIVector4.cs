using Raylib_cs;
using Library.Packaging;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableIVector4 : CodeVariableBase
{
    public readonly int[] Value = new int[4];

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Raylib.SetShaderValue(shader, variableLocation, Value, ShaderUniformDataType.IVec4);
    }
}