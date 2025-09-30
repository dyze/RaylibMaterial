using Library.CodeVariable;
using Library.Helpers;
using NLog;
using System.Text.RegularExpressions;

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
        var type = TypeConvertors.StringToType(item.Type);
        if (type != null)
        {
            // Special case for colors. It will change the way to edit the value (color picker)
            var nameLower = item.Name.ToLower();
            if (type == typeof(CodeVariableVector4) && nameLower.Contains("color") ||
                nameLower.StartsWith("col"))
                type = typeof(CodeVariableColor);

            var uniformDescription = GetUniformDescription(item.Name);
            var internallyHandled = false;
            if (uniformDescription != null)
            {
                internallyHandled = true;
                Logger.Error($"{item.Name} is internally handled");
            }

            var variable = CodeVariableFactory.Build(type);
            variable.Internal = internallyHandled;
            variables.Add(item.Name, variable);
        }
        else
        {
            var variable = CodeVariableFactory.Build(typeof(CodeVariableUnsupported));
            variables.Add(item.Name, variable);

            Logger.Error($"{item.Type} not supported");
        }
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

    private string? GetUniformDescription(string name)
    {
        Dictionary<string, string> internalUniforms = new()
        {
            { "mvp", "model-view-projection matrix" },
            { "matView", "view matrix" },
            { "matProjection", "projection matrix" },
            { "matModel", "model matrix" },
            { "matNormal", "normal matrix (transpose(inverse(matModelView))" },
            { "colDiffuse", "color diffuse (base tint color, multiplied by texture color)" },
            { "viewPos", "Location of camera" }
        };

        return internalUniforms.GetValueOrDefault(name);
    }
}