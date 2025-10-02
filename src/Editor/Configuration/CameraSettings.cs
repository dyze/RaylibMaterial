using Newtonsoft.Json;
using System.Numerics;

namespace Editor.Configuration;

public class CameraSettings
{
    [JsonProperty("Angles")] public Vector3 Angles = Vector3.Zero;
    [JsonProperty("Target")] public Vector3 Target = Vector3.Zero;

    public const float MinDistance = 1f;
    public const float MaxDistance = 10f;

    [JsonProperty("Distance")] private float _distance = 5;

    [JsonIgnore]
    public float Distance
    {
        get => _distance;

        set => _distance = Math.Clamp(value, MinDistance, MaxDistance);
    }
}