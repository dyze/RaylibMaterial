using Library.Lighting;
using Raylib_cs;
using System.Numerics;

namespace ConsumerSampleApp.Examples;

abstract class ExampleBase(Configuration.Configuration configuration)
{
    protected readonly Configuration.Configuration Configuration = configuration;

    private readonly List<Light> _lights = [];

    public FileInfo[] Files = [];

    /// <summary>
    /// Get the name of example
    /// </summary>
    /// <returns>the name</returns>
    public abstract string GetName();

    /// <summary>
    /// Get a summary of the example
    /// </summary>
    /// <returns>the summary</returns>
    public abstract string GetSummary();

    /// <summary>
    /// Get a detailed description of the example
    /// </summary>
    /// <returns>the detailed description</returns>
    public abstract string GetDescription();

    public abstract void Init();
    public abstract void Run();
    public abstract void Close();

    public void CreateLights(List<Shader> shaders)
    {
        LightManager.Clear();
        _lights.Clear();

        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(-2, 1, -2),
            Vector3.Zero,
            Color.Yellow,
        4.0f,
            shaders
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(2, 1, 2),
            Vector3.Zero,
            Color.Red,
        4.0f,
            shaders
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(-2, 1, 2),
            Vector3.Zero,
            Color.Green,
        4.0f,
            shaders
        ));
        _lights.Add(LightManager.CreateLight(
            LightType.Point,
            new Vector3(2, 1, -2),
            Vector3.Zero,
            Color.Blue,
            4.0f,
            shaders
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
        foreach (var light in _lights)
        {
            LightManager.UpdateLightValues(light);
        }
    }

    public void EnumerateMaterials()
    {
        var di = new DirectoryInfo(Configuration.MaterialFolderPath);

        Files = di.GetFiles("*.mat", SearchOption.AllDirectories);
    }
}