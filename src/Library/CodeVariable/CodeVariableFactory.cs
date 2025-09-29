namespace Library.CodeVariable;


public static class CodeVariableFactory
{
    public static CodeVariableBase Build(Type type)
    {

        if (type == typeof(CodeVariableFloat))
            return new CodeVariableFloat();
        else
        if (type == typeof(CodeVariableTexture))
            return new CodeVariableTexture();
        else
        if (type == typeof(CodeVariableColor))
            return new CodeVariableColor();
        else
        if (type == typeof(CodeVariableVector3))
            return new CodeVariableVector3();
        else
        if (type == typeof(CodeVariableVector4))
            return new CodeVariableVector4();

        throw new TypeAccessException($"{type} can't be used");
    }
}
