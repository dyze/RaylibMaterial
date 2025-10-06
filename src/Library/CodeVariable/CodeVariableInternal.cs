using Library.Packaging;
using NLog;
using Raylib_cs;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableInternal : CodeVariableBase
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Apply(IMaterial material1, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        Logger.Error($"{variableName} is handled internally");
    }
}