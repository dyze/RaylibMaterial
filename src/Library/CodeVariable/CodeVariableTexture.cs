using Newtonsoft.Json;
using Raylib_cs;
using System.ComponentModel.DataAnnotations;
using Library.Helpers;
using Library.Packaging;
using NLog;

namespace Library.CodeVariable;

[Serializable]
public class CodeVariableTexture : CodeVariableBase
{
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    [Required][JsonProperty("Value")] public string Value { get; set; } = "";
    [Required][JsonProperty("MaterialMapIndex")] public MaterialMapIndex? MaterialMapIndex { get; set; }

    public override void Apply(IMaterial material, Shader shader, Material raylibMaterial, string variableName, int variableLocation)
    {
        if (MaterialMapIndex == null)
            Logger.Debug($"{variableName}: materialMapIndex not set");
        else
            SetUniformTexture(material,
                shader,
                variableName,
                Value,
                raylibMaterial,
                MaterialMapIndex.Value);
    }

    private void SetUniformTexture(IMaterial material, 
        Shader shader, 
        string variableName,
        string fileName,
        Material raylibMaterial,
        MaterialMapIndex materialMapIndex)
    {
        Logger.Trace("SetUniformTexture...");


        if (fileName == "")
        {
            Logger.Trace("No filename set");
            return;
        }

        var extension = Path.GetExtension(fileName);
        if (extension == null)
            throw new NullReferenceException($"No file extension found in {fileName}");

        var file = material.GetFile(FileType.Image, fileName);
        if (file == null)
            throw new NullReferenceException($"No file {fileName} found");

        var image = Raylib.LoadImageFromMemory(extension, file); // ignore period
        if (Raylib.IsImageValid(image) == false)
        {
            Logger.Debug($"image {fileName} is not valid");
            return;
        }

        var texture = Raylib.LoadTextureFromImage(image);

        Raylib.UnloadImage(image);

        if (Raylib.IsTextureValid(texture) == false)
        {
            Logger.Debug($"texture {variableName} is not valid");
            return;
        }

        unsafe
        {
            var index = TypeConvertors.MaterialMapIndexToShaderLocationIndex(materialMapIndex);
            if (index == null)
            {
                Logger.Debug($"ShaderLocationIndex for {materialMapIndex} not found");
                return;
            }

            shader.Locs[(int)index] = Raylib.GetShaderLocation(shader, variableName);
        }

        Raylib.SetMaterialTexture(ref raylibMaterial, materialMapIndex, texture);
        Logger.Trace($"{variableName}={fileName}, materialMapIndex={materialMapIndex}");
    }
}