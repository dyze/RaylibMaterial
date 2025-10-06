using Library.CodeVariable;
using System.Numerics;
using Library.Lighting;
using Raylib_cs;

namespace Library.Helpers;

public static class TypeConvertors
{
    public static Type? StringToStorageType(string input)
    {
        Dictionary<string, Type> table = new()
        {
            { "int", typeof(CodeVariableInt) },
            { "float", typeof(CodeVariableFloat) },
            { "vec2", typeof(CodeVariableVector2) },
            { "vec3", typeof(CodeVariableVector3) },
            { "vec4", typeof(CodeVariableVector4) },
            { "mat4", typeof(CodeVariableMatrix4x4) },
            { "sampler2D", typeof(CodeVariableTexture) },
        };

        return table.GetValueOrDefault(input);
    }

    public static Dictionary<string, MaterialMapIndex>  StringToMaterialMapIndexTable = new()
    {
        { "texture0", MaterialMapIndex.Albedo }, // Also called diffuse
        { "texture1", MaterialMapIndex.Metalness }, // Also called Specular
        { "texture2", MaterialMapIndex.Normal },
        { "texture3", MaterialMapIndex.Roughness },
        { "texture4", MaterialMapIndex.Occlusion },
        { "texture5", MaterialMapIndex.Emission },
        { "texture6", MaterialMapIndex.Height },
        { "texture7", MaterialMapIndex.Cubemap },
        { "texture8", MaterialMapIndex.Irradiance },
        { "texture9", MaterialMapIndex.Prefilter },
        { "texture10", MaterialMapIndex.Brdf },
    };

    public static MaterialMapIndex? StringToMaterialMapIndex(string input)
    {
        return StringToMaterialMapIndexTable.GetValueOrDefault(input);
    }

    public static Dictionary<MaterialMapIndex, ShaderLocationIndex> MaterialMapIndexToShaderLocationIndexTable = new()
    {
        { MaterialMapIndex.Albedo, ShaderLocationIndex.MapAlbedo }, // Also called diffuse
        { MaterialMapIndex.Metalness, ShaderLocationIndex.MapMetalness }, // Also called Specular
        { MaterialMapIndex.Normal, ShaderLocationIndex.MapNormal },
        { MaterialMapIndex.Roughness, ShaderLocationIndex.MapRoughness },
        { MaterialMapIndex.Occlusion, ShaderLocationIndex.MapOcclusion },
        { MaterialMapIndex.Emission, ShaderLocationIndex.MapEmission },
        { MaterialMapIndex.Height, ShaderLocationIndex.MapHeight },
        { MaterialMapIndex.Cubemap, ShaderLocationIndex.MapCubemap },
        { MaterialMapIndex.Irradiance, ShaderLocationIndex.MapIrradiance },
        { MaterialMapIndex.Prefilter, ShaderLocationIndex.MapPrefilter },
        { MaterialMapIndex.Brdf, ShaderLocationIndex.MapBrdf },
    };

    public static ShaderLocationIndex? MaterialMapIndexToShaderLocationIndex(MaterialMapIndex materialMapIndex)
    {
        return MaterialMapIndexToShaderLocationIndexTable.GetValueOrDefault(materialMapIndex);
    }

    public class UniformDescription
    {
        public UniformDescription(Type? type, bool internalHandled, string description)
        {
            InternalHandled = internalHandled;
            Description = description;
            Type = type;
        }

        public bool InternalHandled { get; private set; }
        public string Description { get; private set; }
        public Type? Type { get; private set; }
    }

    public static UniformDescription? GetUniformDescription(string name)
    {
        Dictionary<string, UniformDescription> internalUniforms = new()
        {
            { "mvp", new UniformDescription(typeof(CodeVariableMatrix4x4), true, "model-view-projection matrix") },
            { "matView", new UniformDescription(typeof(CodeVariableMatrix4x4), true, "view matrix") },
            { "matProjection", new UniformDescription(typeof(CodeVariableMatrix4x4), true, "projection matrix") },
            { "matModel", new UniformDescription(typeof(CodeVariableMatrix4x4), true, "model matrix") },
            { "matNormal",new UniformDescription(typeof(CodeVariableMatrix4x4),  true, "normal matrix (transpose(inverse(matModelView))") },
            { "colDiffuse",new UniformDescription(typeof(CodeVariableVector4),  true, "color diffuse (base tint color, multiplied by texture color)") },
            { "viewPos", new UniformDescription(typeof(CodeVariableVector3),  true, "Location of camera") },
            { "lights", new UniformDescription(null, true, "Lights in our scene") },
            { "texture0", new UniformDescription(typeof(CodeVariableTexture),  false, "Albedo, also called diffuse") },
            { "texture1", new UniformDescription(typeof(CodeVariableTexture),  false, "Metalness, also called Specular") },
            { "texture2", new UniformDescription(typeof(CodeVariableTexture),  false, "Normal") },
            { "texture3", new UniformDescription(typeof(CodeVariableTexture),  false, "Roughness") },
            { "texture4", new UniformDescription(typeof(CodeVariableTexture),  false, "Occlusion") },
            { "texture5", new UniformDescription(typeof(CodeVariableTexture),  false, "Emission") },
            { "texture6", new UniformDescription(typeof(CodeVariableTexture),  false, "Height") },
            { "texture7", new UniformDescription(typeof(CodeVariableTexture),  false, "Cubemap") },
            { "texture8", new UniformDescription(typeof(CodeVariableTexture),  false, "Irradiance") },
            { "texture9", new UniformDescription(typeof(CodeVariableTexture),  false, "Prefilter") },
            { "texture10", new UniformDescription(typeof(CodeVariableTexture),  false, "Brdf") }
        };

        return internalUniforms.GetValueOrDefault(name);
    }

    public static Vector4 ColorToVector4(Color src)
    {
        return new Vector4((float)src.R / (float)byte.MaxValue, (float)src.G / (float)byte.MaxValue,
            (float)src.B / (float)byte.MaxValue, (float)src.A / (float)byte.MaxValue);
    }
}