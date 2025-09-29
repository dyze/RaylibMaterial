using Library.CodeVariable;
using Library.Helpers;
using NLog;
using System.Text.RegularExpressions;

namespace Library;

public class ShaderCode(string code)
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool NeedsRebuild = false;
    public string Code = code;
    public bool IsValid { get; set; } = false;

    /// <summary>
    /// List of uniforms detected inside the code
    /// </summary>
    public Dictionary<string, CodeVariableBase> Variables = [];

    public void ParseVariables()
    {
        Variables = _ParseVariables();
    }

    private Dictionary<string, CodeVariableBase> _ParseVariables()
    {
        var currentPosition = Code;

        var result = new Dictionary<string, CodeVariableBase>();


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


            var type = TypeConvertors.StringToType(typeString);
            if (type != null)
            {
                // Special case for colors. It will change the way to edit the value (color picker)
                var nameLower = name.ToLower();
                if (type == typeof(CodeVariableVector4) && nameLower.Contains("color") ||
                    nameLower.StartsWith("col"))
                    type = typeof(CodeVariableColor);

                var variable = CodeVariableFactory.Build(type);
                result.Add(name, variable);
            }
            else
                Logger.Error($"{typeString} not supported");
        }

        return result;
    }
}