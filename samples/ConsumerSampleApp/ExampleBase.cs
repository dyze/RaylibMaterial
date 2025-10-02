using Library.Lighting;
using Raylib_cs;
using System.Numerics;

namespace ConsumerSampleApp;

abstract class ExampleBase
{
    public readonly string _materialDirectoryPath = "../../../../../materials";
    public readonly string _modelDirectoryPath = "../../../../../src/Editor/resources/models/glb";      //TODO move to a better shared place

    private readonly List<Light> _lights = [];
    public Shader? CurrentShader;

    public abstract void Init();
    public abstract void Run();
    public abstract void Close();

    public void CreateLights()
    {
        if (CurrentShader.HasValue == false)
            throw new NullReferenceException("_currentShader is null");

        LightManager.Clear();
        _lights.Clear();

        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(-2, 1, -2),
            Vector3.Zero,
            Color.Yellow,
        4.0f,
            [CurrentShader.Value]
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(2, 1, 2),
            Vector3.Zero,
            Color.Red,
        4.0f,
            [CurrentShader.Value]
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(-2, 1, 2),
            Vector3.Zero,
            Color.Green,
        4.0f,
            [CurrentShader.Value]
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(2, 1, -2),
            Vector3.Zero,
            Color.Blue,
            4.0f,
            [CurrentShader.Value]
        ));
    }

    public void RenderLights()
    {
        foreach (var light in _lights)
        {
            Raylib.DrawSphereEx(light.Position, 0.2f, 8, 8, light.Color);
        }
    }

    public void UpdateLights()
    {
        if (CurrentShader.HasValue == false)
            throw new NullReferenceException("_currentShader is null");

        foreach (var light in _lights)
        {
            LightManager.UpdateLightValues(light);
        }
    }
}