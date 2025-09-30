// This is inspired from rlights.h: https://github.com/raysan5/raylib/blob/master/examples/shaders/rlights.h
//
// It is available for free. Do whatever you want with it.
//
// Such implementation only works with provided fs and vs shader files
//
// Author: dyze@dlabs.eu

using System.Numerics;
using Library.Packaging;
using Raylib_cs;

namespace Library.Lighting;

/// <summary>
/// Stores the used in the scene. Light information is transmitted to the shaders included in materials
/// </summary>
public static class LightManager
{
    public const int MaxLights = 4;         // Max dynamic lights supported by shader
    private static int _lightsCount;

    public static void Clear()
    {
        _lightsCount = 0;
    }

    /// <summary>
    /// Create a light and get shader locations
    /// </summary>
    /// <param name="type"></param>
    /// <param name="position"></param>
    /// <param name="target"></param>
    /// <param name="color"></param>
    /// <param name="materials"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public static Light CreateLight(LightType type, Vector3 position, Vector3 target, Color color, List<Shader> shaders)
    {
        Light light = new();

        if (_lightsCount >= MaxLights)
            throw new IndexOutOfRangeException($"Count of lights {_lightsCount} exceeds {MaxLights}");

        light.Enabled = true;
        light.Type = type;
        light.Position = position;
        light.Target = target;
        light.Color = color;

        // names below must match those defined in shader fs files

        var enabledName = $"lights[{_lightsCount}].enabled";
        var typeName = $"lights[{_lightsCount}].type";
        var posName = $"lights[{_lightsCount}].position";
        var targetName = $"lights[{_lightsCount}].target";
        var colorName = $"lights[{_lightsCount}].color";

        foreach (var material in shaders)
        {
            light.EnabledLoc.Add(Raylib.GetShaderLocation(material, enabledName));
            light.TypeLoc.Add(Raylib.GetShaderLocation(material, typeName));
            light.PosLoc.Add(Raylib.GetShaderLocation(material, posName));
            light.TargetLoc.Add(Raylib.GetShaderLocation(material, targetName));
            light.ColorLoc.Add(Raylib.GetShaderLocation(material, colorName));
        }

        light.Shaders = shaders;

        UpdateLightValues(light);

        _lightsCount++;
        
        return light;
    }

    /// <summary>
    /// Send light properties to shader
    ///  NOTE: Light shader locations should be available 
    /// </summary>
    /// <param name="light"></param>
    public static void UpdateLightValues(Light light)
    {
        for (var i = 0; i < light.Shaders.Count(); i++)
        {
            var shader = light.Shaders[i];


            // Send to shader light enabled state, type, position and target
            var enabled = light.Enabled ? 1 : 0;
            Raylib.SetShaderValue(shader, light.EnabledLoc[i], enabled, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(shader, light.TypeLoc[i], light.Type, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(shader, light.PosLoc[i], light.Position, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(shader, light.TargetLoc[i], light.Target, ShaderUniformDataType.Vec3);

            // Send to shader light color values
            var color = new Vector4(light.Color.R / (float)255, light.Color.G / (float)255,
                light.Color.B / (float)255, light.Color.A / (float)255);
            Raylib.SetShaderValue(shader, light.ColorLoc[i], color, ShaderUniformDataType.Vec4);
        }
    }

    //public static void SetAmbientColor(Shader shader, System.Drawing.Color ambientColor)
    //{
    //    var ambientLoc = Raylib.GetShaderLocation(shader, "ambient");

    //    var color = new Vector4(ambientColor.R / (float)255, ambientColor.G / (float)255,
    //        ambientColor.B / (float)255, ambientColor.A / (float)255);

    //    Raylib.SetShaderValue(shader, ambientLoc, color, ShaderUniformDataType.Vec4);
    //}
}