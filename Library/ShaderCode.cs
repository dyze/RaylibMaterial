using System.Numerics;
using System.Text.RegularExpressions;

namespace Library;

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

            var type = StringToType(typeString);
            if (type != null)
                result.Add(new CodeVariable(type, name));
        }

        return result;
    }

    private static Type? StringToType(string input)
    {
        Dictionary<string, Type> table = new()
        {
            { "float", typeof(float) },
            { "vec2", typeof(Vector2) },
            { "vec3", typeof(Vector3) },
            { "vec4", typeof(Vector4) },
            { "int", typeof(int) },
            { "uint", typeof(uint) },
            { "sampler2D", typeof(string) },
        };

        return table.GetValueOrDefault(input);
    }
}