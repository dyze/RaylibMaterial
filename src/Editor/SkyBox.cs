using Editor.Configuration;
using Raylib_cs;

namespace Editor;

/// <summary>
/// This class prepares and holds a sky box model
/// </summary>
internal class SkyBox(EditorConfiguration editorConfiguration)
{
    public Model Model;

    public Model GenerateModel(string filePath)
    {
        Raylib.TraceLog(TraceLogLevel.Info, "GenerateModel...");

        var meshCube = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
        Model = Raylib.LoadModelFromMesh(meshCube);

        var shader = Raylib.LoadShader($"{editorConfiguration.ResourceSkyBoxesFolderPath}/skybox.vert",
            $"{editorConfiguration.ResourceSkyBoxesFolderPath}/skybox.frag");

        Raylib.SetShaderValue(shader,
            Raylib.GetShaderLocation(shader, "environmentMap"),
            MaterialMapIndex.Cubemap,
            ShaderUniformDataType.Int);

        // 0=None, 1=Vertical only, 2=Horizontal only, 3=both 
        Raylib.SetShaderValue(
            shader,
            Raylib.GetShaderLocation(shader, "flipMode"),
            2,
            ShaderUniformDataType.Int
        );

        Raylib.SetMaterialShader(ref Model, 0, ref shader);

        var image = Raylib.LoadImage(filePath);
        var cubeMap = Raylib.LoadTextureCubemap(image, CubemapLayout.AutoDetect);
        Raylib.SetMaterialTexture(ref Model, 0, MaterialMapIndex.Cubemap, ref cubeMap);
        Raylib.UnloadImage(image); // Texture not required anymore, cubemap already generated

        Raylib.TraceLog(TraceLogLevel.Info, "GenerateModel OK");

        return Model;
    }
}