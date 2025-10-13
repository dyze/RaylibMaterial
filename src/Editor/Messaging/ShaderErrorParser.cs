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
///
/// Second case:
/// SHADER: [ID 9] Failed to compile fragment shader code
/// Then:
/// SHADER: [ID 9] Compile error: ERROR: 0:20: 'finalColor' : undeclared identifier
/// ERROR: 0:20: 'assign' :  cannot convert from '4-component vector of highp float' to 'highp float'
/// </summary>
public static class ShaderErrorParser
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Parse(string message, Dictionary<FileId, ShaderCode> shaderCodes)
    {
        if (message.Contains("Failed to compile"))
        {
            FileType? faultyShader;

            if (message.Contains("vertex"))
                faultyShader = FileType.VertexShader;
            else if (message.Contains("fragment"))
                faultyShader = FileType.FragmentShader;
            else
                return;

            var shaderCode = shaderCodes.FirstOrDefault(s => s.Key.FileType == faultyShader);

            var shaderId = ExtractShaderIdFromMessage(message);
            if (shaderId == null)
                return;

            shaderCode.Value.ShaderId = shaderId;

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
                var lineNumber = -1;
                var newPos = -1;

                // Try first case
                var firstCaseOk = FirstCase(message, ref lineNumber, ref newPos);
                if (firstCaseOk == false)
                {
                    // Try second case
                    if (SecondCase(message, ref lineNumber, ref newPos) == false)
                        return;
                }

                message = message.Substring(newPos);


                match = Regex.Match(message, ": ");
                if (match.Success == false)
                    return;

                var posStartMessage = match.Index + match.Length;

                int posEndMessage;
                match = Regex.Match(message, "\n");
                if (match.Success == false)
                    posEndMessage = message.Length;
                else
                    posEndMessage = match.Index + match.Length;

                var errorMessage = message.Substring(posStartMessage, posEndMessage - posStartMessage);
                errorMessage = errorMessage.ReplaceLineEndings("");

                // Sometimes there are duplicated messages.
                // Moreover, several messages can be associated to the same line of code

                if (shaderCode.Value.Errors.TryGetValue(lineNumber, out var existingMessage))
                {
                    if (errorMessage != (string)existingMessage)
                        errorMessage = string.Concat(existingMessage, "\n", errorMessage);
                }

                shaderCode.Value.Errors[lineNumber] = errorMessage;

                message = message.Substring(posEndMessage);
            }

        }
    }

    /// <summary>
    /// Tries to extract the error line number from a message such as "ERROR: 0:20: 'finalColor' : undeclared identifier"
    /// </summary>
    /// <param name="message">the message to analyse</param>
    /// <param name="lineNumber">resulting code line number</param>
    /// <returns>true if message understood and lineNumber extracted</returns>
    private static bool SecondCase(string message, ref int lineNumber, ref int newPos)
    {
        var match = Regex.Match(message, "ERROR: [0-9]*:");
        if (match.Success == false)
            return false;

        var position = match.Index + match.Length;

        match = Regex.Match(message.Substring(position), ":");
        if (match.Success == false)
            return false;

        var pos2 = match.Index + position;

        var sub = message.Substring(position, pos2 - position);

        lineNumber = int.Parse(sub);

        newPos = pos2;

        return true;
    }

    /// <summary>
    /// Tries to extract the error line number from a message such as "0(41) : error C1503: undefined variable "fragNormal""
    /// </summary>
    /// <param name="message">the message to analyse</param>
    /// <param name="lineNumber">resulting code line number</param>
    /// <returns>true if message understood and lineNumber extracted</returns>
    private static bool FirstCase(string message, ref int lineNumber, ref int newPos)
    {
        var match = Regex.Match(message, @"[0-9]\(");
        if (match.Success == false)
            return false;

        var position = match.Index + match.Length;

        match = Regex.Match(message, @"\)");
        if (match.Success == false)
            return false;

        var pos2 = match.Index;

        var sub = message.Substring(position, pos2 - position);

        lineNumber = int.Parse(sub);

        newPos = pos2;

        return true;
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