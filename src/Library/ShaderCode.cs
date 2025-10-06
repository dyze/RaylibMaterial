using Library.CodeVariable;
using Library.Helpers;
using NLog;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using static Library.Helpers.TypeConvertors;

namespace Library;

internal class TypeName
{
    public string Type;
    public string Name;

    public TypeName(string type, string name)
    {
        Type = type;
        Name = name;
    }
}

/// <summary>
/// Stores the code of a shader component
/// It is used also to parse the variables inside that component
/// </summary>
public class ShaderCode
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool NeedsRebuild = true;
    public string Code;
    public bool IsValid { get; set; } = false;

    // key is line number
    public Dictionary<int, object> Errors = [];

    /// <summary>
    /// List of uniforms detected inside the code
    /// </summary>
    public Dictionary<string, CodeVariableBase> Variables = [];

    public int? ShaderId = null;

    /// <summary>
    /// Stores the code of a shader component
    /// It is used also to parse the variables inside that component
    /// </summary>
    /// <param name="code"></param>
    public ShaderCode(string code)
    {
        Code = code;
    }

    public void ParseVariables()
    {
        Variables = _ParseVariables();
    }

    private Dictionary<string, CodeVariableBase> _ParseVariables()
    {
        var currentPosition = Code;

        var variables = new Dictionary<string, CodeVariableBase>();


        while (true)
        {
            var match = Regex.Match(currentPosition, @"^\s*uniform\s", RegexOptions.Multiline);
            if (match.Success == false)
                break;

            var item = ParseUniform(match, ref currentPosition);

            RegisterUniform(item, variables);
        }

        return variables;
    }

    private void RegisterUniform(TypeName item, Dictionary<string, CodeVariableBase> variables)
    {
        var typeInScript = StringToStorageType(item.Type);

        if (typeInScript == null)
        {
            var unsupportedVariable = CodeVariableFactory.Build(typeof(CodeVariableUnsupported));
            variables.Add(item.Name, unsupportedVariable);

            Logger.Error($"{item.Type} not supported");

            return;
        }

        var uniformDescription = GetUniformDescription(item.Name);
        var internallyHandled = false;
        if (uniformDescription != null)
        {
            internallyHandled = uniformDescription.InternalHandled;
            if (internallyHandled)
            {
                Logger.Trace($"{item.Name} is internally handled");

                if (uniformDescription.Type != null &&
                    uniformDescription.Type != typeInScript)
                    throw new TypeAccessException(
                        $"{item.Name} type should be {uniformDescription.Type} and not {typeInScript} ");

                var selectedType = typeof(CodeVariableInternal);
                var internalVariable = CodeVariableFactory.Build(selectedType);
                variables.Add(item.Name, internalVariable);

                return;
            }
        }


        // Special case for colors. It will change the way to edit the value (color picker)
        var nameLower = item.Name.ToLower();
        if (typeInScript == typeof(CodeVariableVector4) && nameLower.Contains("color") ||
            nameLower.StartsWith("col"))
            typeInScript = typeof(CodeVariableColor);

        var variable = CodeVariableFactory.Build(typeInScript);
        variables.Add(item.Name, variable);
    }

    private static TypeName ParseUniform(Match match, ref string currentPosition)
    {
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
        return new TypeName(typeString, name);
    }


}