namespace Library.CodeVariable;


public static class CodeVariableFactory
{
    public static CodeVariableBase Build(Type type)
    {
        if(type.IsSubclassOf(typeof(CodeVariableBase)) == false)
            throw new TypeAccessException($"{type} can't be used");

        var ctor = type.GetConstructor(Type.EmptyTypes);
        var instance = ctor.Invoke(null);
        if(instance == null)
            throw new TypeAccessException($"{type} ctor failed");

        return instance as CodeVariableBase ?? throw new InvalidOperationException();
    }
}
