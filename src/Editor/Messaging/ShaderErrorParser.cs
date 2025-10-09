using Library.Packaging;
using System.Text.RegularExpressions;
using Library;
using NLog;

namespace Editor.Messaging;

/// <summary>
/// This class parse the shader compilation error sent thru Raylib.
///
/// Example of messages:
/// SHADER: [ID 8] Failed to compile vertex shader code
/// Then:
/// SHADER: [ID 8] Compile error: 0(22) : error C0000: syntax error, unexpected identifier, expecting ',' or ';' at token "Normal"
/// 0(41) : error C1503: undefined variable "fragNormal"
/// 0(43) : error C1503: undefined variable "fragNormal"
/// 0(43) : error C1503: undefined variable "fragNormal"
/// 0(45) : error C1503: undefined variable "fragNormal"
/// 0(47) : error C1503: undefined variable "fragNormal"
/// </summary>
public static class ShaderErrorParser
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Parse(string message, Dictionary<FileId, ShaderCode> shaderCodes)
    {
        if (message.Contains("Failed to compile"))
        {
            FileType? faultyShader = null;

            if (message.Contains("vertex"))
            {
                faultyShader = FileType.VertexShader;
            }
            else if (message.Contains("fragment"))
            {
                faultyShader = FileType.FragmentShader;
            }

            if (faultyShader != null)
            {
                var shaderCode = shaderCodes.FirstOrDefault(s => s.Key.FileType == faultyShader);

                var shaderId = ExtractShaderIdFromMessage(message);
                if (shaderId == null)
                    return;
                shaderCode.Value.ShaderId = shaderId;
            }
        }
        else
        if (message.Contains("Compile error"))
        {
            var shaderId = ExtractShaderIdFromMessage(message);
            if (shaderId == null)
                return;

            var shaderCode = shaderCodes.FirstOrDefault(s => s.Value.ShaderId == shaderId);

            var match = Regex.Match(message, "Compile error: ");
            if (match.Success == false)
                return;

            message = message.Substring(match.Index
                                        + match.Length);
            while (true)
            {
               match = Regex.Match(message, @"[0-9]\(");
                if (match.Success == false)
                    return;

                var position = match.Index + match.Length;

                match = Regex.Match(message, @"\)");
                if (match.Success == false)
                    return;

                var pos2 = match.Index;

                var sub = message.Substring(position, pos2 - position);

                var lineNumber = int.Parse(sub);

                match = Regex.Match(message, @" : ");
                if (match.Success == false)
                    return;

                var posStartMessage = match.Index + match.Length;

                int posEndMessage;
                match = Regex.Match(message, "\n");
                if (match.Success == false)
                    posEndMessage = message.Length;
                else
                    posEndMessage = match.Index + match.Length;

                var errorMessage = message.Substring(posStartMessage, posEndMessage-posStartMessage);
                errorMessage = errorMessage.ReplaceLineEndings("");

                // TryAdd because I already saw duplicated messages.
                shaderCode.Value.Errors.TryAdd(lineNumber, errorMessage);

                message = message.Substring(posEndMessage);
            }

        }
    }

    private static int? ExtractShaderIdFromMessage(string message)
    {
        // get id to track other messages related to that shader
        var match = Regex.Match(message, @"\x5BID ");
        if (match.Success == false)
            return null;

        var position = match.Index + match.Length;

        match = Regex.Match(message, @"\x5D");
        if (match.Success == false)
            return null;

        var pos2 = match.Index;

        var sub = message.Substring(position, pos2 - position);

        int? shaderId = null;

        try
        {
            shaderId = int.Parse(sub);
        }
        catch (FormatException e)
        {
            Logger.Error(e);
        }

        return shaderId;
    }
}