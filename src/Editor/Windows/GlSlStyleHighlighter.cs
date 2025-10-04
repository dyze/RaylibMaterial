using ImGuiColorTextEditNet;
using ImGuiColorTextEditNet.Syntax;

namespace Editor.Windows;

/// <summary>
/// A syntax highlighter for glsl language.
/// </summary>
public class GlSlStyleHighlighter : ISyntaxHighlighter
{
    static readonly object DefaultState = new();
    static readonly object MultiLineCommentState = new();
    readonly SimpleTrie<Identifier> _identifiers;

    record Identifier(PaletteIndex Color)
    {
        public string Declaration = "";
    }

    /// <summary>
    /// Creates a new instance of the ShaderStyleHighlighter.
    /// </summary>
    public GlSlStyleHighlighter()
    {
        var language = GlSl();

        _identifiers = new();
        if (language.Keywords != null)
            foreach (var keyword in language.Keywords)
                _identifiers.Add(keyword, new(PaletteIndex.Keyword));

        if (language.Identifiers != null)
        {
            foreach (var name in language.Identifiers)
            {
                var identifier = new Identifier(PaletteIndex.KnownIdentifier)
                {
                    Declaration = "Built-in function",
                };
                _identifiers.Add(name, identifier);
            }
        }
    }

    /// <summary>Indicates whether the highlighter supports auto-indentation.</summary>
    public bool AutoIndentation => true;

    /// <summary>The maximum number of lines that can be processed in a single frame.</summary>
    public int MaxLinesPerFrame => 1000;

    /// <summary>Retrieves the tooltip for a given identifier.</summary>
    public string? GetTooltip(string id)
    {
        var info = _identifiers.Get(id);
        return info?.Declaration;
    }

    /// <summary>Colorizes a line of text based on GlSl/GlSl++ syntax rules.</summary>
    public object Colorize(Span<Glyph> line, object? state)
    {
        for (int i = 0; i < line.Length;)
        {
            int result = Tokenize(line[i..], ref state);
            Util.Assert(result != 0);

            if (result == -1)
            {
                line[i] = new(line[i].Char, PaletteIndex.Default);
                i++;
            }
            else
                i += result;
        }

        return state ?? DefaultState;
    }

    int Tokenize(Span<Glyph> span, ref object? state)
    {
        int i = 0;

        // Skip leading whitespace
        while (i < span.Length && span[i].Char is ' ' or '\t')
            i++;

        if (i > 0)
            return i;

        int result;
        if ((result = TokenizeMultiLineComment(span, ref state)) != -1)
            return result;

        if ((result = TokenizeSingleLineComment(span)) != -1)
            return result;

        if ((result = TokenizePreprocessorDirective(span)) != -1)
            return result;

        if ((result = TokenizeCStyleString(span)) != -1)
            return result;

        if ((result = TokenizeCStyleCharacterLiteral(span)) != -1)
            return result;

        if ((result = TokenizeCStyleIdentifier(span)) != -1)
            return result;

        if ((result = TokenizeCStyleNumber(span)) != -1)
            return result;

        if ((result = TokenizeCStylePunctuation(span)) != -1)
            return result;

        return -1;
    }

    static int TokenizeMultiLineComment(Span<Glyph> span, ref object? state)
    {
        int i = 0;
        if (
            state != MultiLineCommentState
            && (span[i].Char != '/' || 1 >= span.Length || span[1].Char != '*')
        )
        {
            return -1;
        }

        state = MultiLineCommentState;
        for (; i < span.Length; i++)
        {
            span[i] = new(span[i].Char, PaletteIndex.MultiLineComment);
            if (span[i].Char == '*' && i + 1 < span.Length && span[i + 1].Char == '/')
            {
                i++;
                span[i] = new(span[i].Char, PaletteIndex.MultiLineComment);
                state = DefaultState;
                return i;
            }
        }

        return i;
    }

    static int TokenizeSingleLineComment(Span<Glyph> span)
    {
        if (span[0].Char != '/' || 1 >= span.Length || span[1].Char != '/')
            return -1;

        for (int i = 0; i < span.Length; i++)
            span[i] = new(span[i].Char, PaletteIndex.Comment);

        return span.Length;
    }

    static int TokenizePreprocessorDirective(Span<Glyph> span)
    {
        if (span[0].Char != '#')
            return -1;

        for (int i = 0; i < span.Length; i++)
            span[i] = new(span[i].Char, PaletteIndex.Preprocessor);

        return span.Length;
    }

    // csharpier-ignore-start
    static LanguageDefinition GlSl() =>
        new("GlSl")
        {
            // taken from https://wikis.khronos.org/opengl/Data_Type_(GLSL)
            Keywords =
            [
                // c like keywords
                "void", "main",
                // glsl specifics
                "in", "out", "uniform",
                // types
                //   scalars
                "bool", "float", "double", "int", "uint",
                //   vectors
                "bvec2", "bvec3", "bvec4",
                "ivec2", "ivec3", "ivec4",
                "uvec2", "uvec3", "uvec4",
                "vec2", "vec3", "vec4",
                "dvec2", "dvec3", "dvec4",
                // Swizzling
                //TODO add all combinations
                "xy", "yz", "xz",
                // Matrices
                "mat1x1", "mat1x2", "mat1x3", "mat1x4",
                "mat2x1", "mat2x2", "mat2x3", "mat2x4",
                "mat3x1", "mat3x2", "mat3x3", "mat3x4",
                "mat4x1", "mat4x2", "mat4x3", "mat4x4",
                "mat1", "mat2", "mat3", "mat4",
                // Sampler types
                //TODO add all combinations
                //  Samplers
                "sampler2D",
                //  Images
                //TODO add all combinations
                //  Atomic counters
                //TODO add all combinations
                "Light",
                // Structs
                "struct"
                //"auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else",
                //"enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register",
                //"restrict", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union",
                //"unsigned", "void", "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic",
                //"_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local",
            ],
            Identifiers =
            [
                // functions. https://registry.khronos.org/OpenGL-Refpages/gl4/
                // a
                "abs", "acos", "acosh", "all", "any", "asin", "asinh", "atan", "atanh", "atomicAdd", "atomicAnd",
                "atomicCompSwap", "atomicCounter", "atomicCounterDecrement", "atomicCounterIncrement", "atomicExchange",
                "atomicMax", "atomicMin", "atomicOr", "atomicXor",
                // b
                "barrier", "bitCount", "bitfieldExtract", "bitfieldInsert", "bitfieldReverse",
                // c
                "ceil", "clamp", "cos", "cosh", "cross",
                // d
                "degrees", "determinant", "dFdx", "dFdxCoarse", "dFdxFine", "dFdy", "dFdyCoarse", "dFdyFine",
                "distance", "dot",
                // e
                "EmitStreamVertex", "EmitVertex", "EndPrimitive", "EndStreamPrimitive", "equal", "exp", "exp2",
                // f
                "faceforward", "findLSB", "findMSB", "floatBitsToInt", "floatBitsToUint", "floor", "fma", "fract",
                "frexp", "fwidth", "fwidthCoarse", "fwidthFine",
                // g
                "gl_ClipDistance", "gl_CullDistance", "gl_FragCoord", "gl_FragDepth", "gl_FrontFacing",
                "gl_GlobalInvocationID", "gl_HelperInvocation", "gl_InstanceID", "gl_InvocationID", "gl_Layer",
                "gl_LocalInvocationID", "gl_LocalInvocationIndex", "gl_NumSamples", "gl_NumWorkGroups",
                "gl_PatchVerticesIn", "gl_PointCoord", "gl_PointSize", "gl_Position", "gl_PrimitiveID",
                "gl_PrimitiveIDIn", "gl_SampleID", "gl_SampleMask", "gl_SampleMaskIn", "gl_SamplePosition",
                "gl_TessCoord", "gl_TessLevelInner", "gl_TessLevelOuter", "gl_VertexID", "gl_ViewportIndex",
                "gl_WorkGroupID", "gl_WorkGroupSize", "glFramebufferParameteri", "glNamedFramebufferParameteri",
                "greaterThan", "greaterThanEqual", "groupMemoryBarrier",
                // i
                "imageAtomicAdd", "imageAtomicAnd", "imageAtomicCompSwap", "imageAtomicExchange", "imageAtomicMax",
                "imageAtomicMin", "imageAtomicOr", "imageAtomicXor", "imageLoad", "imageSamples", "imageSize",
                "imageStore", "imulExtended", "intBitsToFloat", "interpolateAtCentroid", "interpolateAtOffset",
                "interpolateAtSample", "inverse", "inversesqrt", "isinf", "isnan",
                // l
                "ldexp", "length", "lessThan", "lessThanEqual", "log", "log2",
                // m
                "matrixCompMult", "max", "memoryBarrier", "memoryBarrierAtomicCounter", "memoryBarrierBuffer",
                "memoryBarrierImage", "memoryBarrierShared", "min", "mix", "mod", "modf",
                // n
                "noise", "noise1", "noise2", "noise3", "noise4", "normalize", "not", "notEqual",
                // o
                "outerProduct",
                // p
                "packDouble2x32", "packHalf2x16", "packSnorm2x16", "packSnorm4x8", "packUnorm", "packUnorm2x16",
                "packUnorm4x8", "pow",
                // r
                "radians", "reflect", "refract", "round", "roundEven",
                // s
                "sign", "sin", "sinh", "smoothstep", "sqrt", "step",
                // t
                "tan", "tanh", "texelFetch", "texelFetchOffset", "texture", "textureGather", "textureGatherOffset",
                "textureGatherOffsets", "textureGrad", "textureGradOffset", "textureLod", "textureLodOffset",
                "textureOffset", "textureProj", "textureProjGrad", "textureProjGradOffset", "textureProjLod",
                "textureProjLodOffset", "textureProjOffset", "textureQueryLevels", "textureQueryLod", "textureSamples",
                "textureSize", "transpose", "trunc",
                // u
                "uaddCarry", "uintBitsToFloat", "umulExtended", "unpackDouble2x32", "unpackHalf2x16", "unpackSnorm2x16",
                "unpackSnorm4x8", "unpackUnorm", "unpackUnorm2x16", "unpackUnorm4x8", "usubBorr",

                // built in variables. https://wikis.khronos.org/opengl/Built-in_Variable_(GLSL)
                //   Vertex shader inputs
                "gl_VertexID", "gl_InstanceID", "gl_DrawID", "gl_BaseVertex", "gl_BaseInstance",
                //   Vertex shader outputs
                "gl_Position", "gl_PointSize", "gl_ClipDistance",
                //   Tessellation control shader inputs
                "gl_PatchVerticesIn", "gl_PrimitiveID", "gl_InvocationID",
                //   Tessellation control shader outputs
                //   Tessellation evaluation shader inputs
                //   Tessellation evaluation shader outputs
                //   Geometry shader inputs
                //   Geometry shader outputs
                //   Geometry shader outputs
                //   Fragment shader inputs
                "gl_FragCoord", "gl_FrontFacing", "gl_PointCoord",
                "gl_SampleID", "gl_SamplePosition", "gl_SampleMaskIn",
                "gl_ClipDistance", "gl_PrimitiveID",
                "gl_Layer", "gl_ViewportIndex",
                //   Fragment shader outputs
                "gl_FragDepth",
                //   Compute shader inputs
                //   Compute shader other variables
                //   Shader uniforms
                //   Constants
                //TODO add all combinations
            ]
        };

    // csharpier-ignore-end

    static int TokenizeCStyleString(Span<Glyph> input)
    {
        if (input[0].Char != '"')
            return -1; // No opening quotes

        for (int i = 1; i < input.Length; i++)
        {
            var c = input[i].Char;

            // handle end of string
            if (c == '"')
            {
                for (int j = 0; j < i; j++)
                    input[i] = new(c, PaletteIndex.String);

                return i;
            }

            // handle escape character for "
            if (c == '\\' && i + 1 < input.Length && input[i + 1].Char == '"')
                i++;
        }

        return -1; // No closing quotes
    }

    static int TokenizeCStyleCharacterLiteral(Span<Glyph> input)
    {
        int i = 0;

        if (input[i++].Char != '\'')
            return -1;

        if (i < input.Length && input[i].Char == '\\')
            i++; // handle escape characters

        i++; // Skip actual char

        // handle end of character literal
        if (i >= input.Length || input[i].Char != '\'')
            return -1;

        for (int j = 0; j < i; j++)
            input[j] = new(input[j].Char, PaletteIndex.CharLiteral);

        return i;
    }

    int TokenizeCStyleIdentifier(Span<Glyph> input)
    {
        int i = 0;

        var c = input[i].Char;
        if (!char.IsLetter(c) && c != '_')
            return -1;

        i++;

        for (; i < input.Length; i++)
        {
            c = input[i].Char;
            if (c != '_' && !char.IsLetterOrDigit(c))
                break;
        }

        var info = _identifiers.Get<Glyph>(input[..i], x => x.Char);

        for (int j = 0; j < i; j++)
            input[j] = new(input[j].Char, info?.Color ?? PaletteIndex.Identifier);

        return i;
    }

    static int TokenizeCStyleNumber(Span<Glyph> input)
    {
        var i = 0;
        var c = input[i].Char;

        var startsWithNumber = char.IsNumber(c);

        if (c != '+' && c != '-' && !startsWithNumber)
            return -1;

        i++;

        var hasNumber = startsWithNumber;
        while (i < input.Length && char.IsNumber(input[i].Char))
        {
            hasNumber = true;
            i++;
        }

        if (!hasNumber)
            return -1;

        var isFloat = false;
        var isHex = false;
        var isBinary = false;

        if (i < input.Length)
        {
            if (input[i].Char == '.')
            {
                isFloat = true;

                i++;
                while (i < input.Length && char.IsNumber(input[i].Char))
                    i++;
            }
            else if (input[i].Char is 'x' or 'X' && i == 1 && input[i].Char == '0')
            {
                // hex formatted integer of the type 0xef80
                isHex = true;

                i++;
                for (; i < input.Length; i++)
                {
                    c = input[i].Char;
                    if (
                        !char.IsNumber(c)
                        && c is not (>= 'a' and <= 'f')
                        && c is not (>= 'A' and <= 'F')
                    )
                    {
                        break;
                    }
                }
            }
            else if (input[i].Char is 'b' or 'B' && i == 1 && input[i].Char == '0')
            {
                // binary formatted integer of the type 0b01011101

                isBinary = true;

                i++;
                for (; i < input.Length; i++)
                {
                    c = input[i].Char;
                    if (c != '0' && c != '1')
                        break;
                }
            }
        }

        if (!isHex && !isBinary)
        {
            // floating point exponent
            if (i < input.Length && input[i].Char is 'e' or 'E')
            {
                isFloat = true;

                i++;

                if (i < input.Length && input[i].Char is '+' or '-')
                    i++;

                var hasDigits = false;
                while (i < input.Length && input[i].Char is >= '0' and <= '9')
                {
                    hasDigits = true;
                    i++;
                }

                if (!hasDigits)
                    return -1;
            }

            // single precision floating point type
            if (i < input.Length && input[i].Char == 'f')
                i++;
        }

        if (!isFloat)
        {
            // integer size type
            while (i < input.Length && input[i].Char is 'u' or 'U' or 'l' or 'L')
                i++;
        }

        return i;
    }

    static int TokenizeCStylePunctuation(Span<Glyph> input)
    {
        // csharpier-ignore-start
        switch (input[0].Char)
        {
            case '[':
            case ']':
            case '{':
            case '}':
            case '(':
            case ')':
            case '-':
            case '+':
            case '<':
            case '>':
            case '?':
            case ':':
            case ';':
            case '!':
            case '%':
            case '^':
            case '&':
            case '|':
            case '*':
            case '/':
            case '=':
            case '~':
            case ',':
            case '.':
                input[0] = new(input[0].Char, PaletteIndex.Punctuation);
                return 1;

            default:
                return -1;
        }
        // csharpier-ignore-end
    }
}