using Newtonsoft.Json;
using System.Numerics;

namespace Editor.Configuration;

public class CameraSettings
{
    public const float MinDistance = 5;

    [JsonProperty("Angles")] public Vector3 Angles = Vector3.Zero;
    [JsonProperty("Target")] public Vector3 Target = Vector3.Zero;
    [JsonProperty("Distance")] public float Distance = 5;
}