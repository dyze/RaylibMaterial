using System.Numerics;
using Raylib_cs;

namespace Library.Lighting;

// Must match values in fs shader file
public enum LightType
{
    Directional = 0,
    Point = 1,
    Spot
}

public class Light
{
    public bool Enabled;
    public LightType Type;
    public Vector3 Position;
    public Vector3 Target;
    public Color Color;
    public float Intensity;


    // Shader locations
    public List<int> EnabledLoc = [];
    public List<int> TypeLoc = [];
    public List<int> PosLoc = [];
    public List<int> TargetLoc = [];
    public List<int> ColorLoc = [];
    public List<int> IntensityLoc = [];

    // Shaders that use light
    public List<Shader> Shaders = [];

    public Light(LightType type, Vector3 position, Vector3 target, Color color, float intensity)
    {
        Type = type;
        Position = position;
        Target = target;
        Color = color;
        Intensity = intensity;
    }
};