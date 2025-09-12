using Raylib_cs;
using System.Text.RegularExpressions;

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

public class ShaderCode(string code)
{
    public bool Modified = false;
    public string Code = code;
    public bool IsValid { get; set; } = true;
    public List<CodeVariable> Variables = [];

    public void ParseVariables()
    {
        Variables = _ParseVariables();
    }

    private List<CodeVariable> _ParseVariables()
    {
        var currentPosition = Code;

        var result = new List<CodeVariable>();


        while (true)
        {
            var match = Regex.Match(currentPosition, @"^\s*uniform\s", RegexOptions.Multiline);
            if (match.Success == false)
                break;

            // Jump over "uniform"
            currentPosition = currentPosition.Substring(match.Index + match.Length);

            // Parse type
            match = Regex.Match(currentPosition, @"[a-zA-Z0-9]*\s*", RegexOptions.Multiline);
            if (match.Success == false)
                throw new Exception("type missing");

            var typeString = match.Value.Trim();

            // Jump over type
            currentPosition = currentPosition.Substring(match.Index + match.Length);

            // Parse name
            match = Regex.Match(currentPosition, @"[a-zA-Z0-9_]*\s*", RegexOptions.Multiline);
            var name = match.Value.Trim();
            if (match.Success == false)
                throw new Exception("name missing");

            // Jump over type
            currentPosition = currentPosition.Substring(match.Index + match.Length);

            // Look for ';'
            match = Regex.Match(currentPosition, @";", RegexOptions.Multiline);
            if (match.Success == false)
                throw new Exception("; missing");

            // Jump over type
            currentPosition = currentPosition.Substring(match.Index + match.Length);

            Console.WriteLine($"{typeString} {name}");

            var type = stringToShaderUniformDataType(typeString);
            if(type != null)
                result.Add(new CodeVariable(type.Value, name));
        }

        return result;
    }

    private static ShaderUniformDataType? stringToShaderUniformDataType(string input)
    {
        Dictionary<string, ShaderUniformDataType> table = new(){
            { "float", ShaderUniformDataType.Float },
            { "vec2", ShaderUniformDataType.Vec2 },
            { "vec3", ShaderUniformDataType.Vec3 },
            { "vec4", ShaderUniformDataType.Vec4 },
            { "int", ShaderUniformDataType.Int },
            { "uint", ShaderUniformDataType.UInt },
            { "sampler2D", ShaderUniformDataType.Sampler2D },
        };

        if (table.TryGetValue(input, out var type))
            return type;
        else
            return null;
    }
}