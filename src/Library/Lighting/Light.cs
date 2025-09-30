using System.Numerics;
using Library.Packaging;
using Raylib_cs;

// Must match values in fs shader file
public enum LightType
{
    Directional = 0,
    Point = 1
}

public class Light
{
    public LightType Type;
    public Vector3 Position;
    public Vector3 Target;
    public Color Color;
    public bool Enabled;

    // Shader locations
    public List<int> EnabledLoc = [];
    public List<int> TypeLoc = [];
    public List<int> PosLoc = [];
    public List<int> TargetLoc = [];
    public List<int> ColorLoc = [];

    // Shaders that use light
    public List<Shader> Shaders;
};