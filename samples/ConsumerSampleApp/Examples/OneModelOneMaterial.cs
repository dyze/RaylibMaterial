using ImGuiNET;
using Library.Lighting;
using Library.Packaging;
using Raylib_cs;
using System.Numerics;

namespace ConsumerSampleApp.Examples;

internal class OneModelOneMaterial : ExampleBase
{
    private Model _currentModel;
    private readonly List<Light> _lights = [];
    private Camera3D _camera;
    private MaterialPackage? _materialPackage;

    private Shader? _currentShader;

    public OneModelOneMaterial(Configuration.Configuration configuration)
        : base(configuration)
    {
    }

    public override string GetName()
    {
        return "One model - One material";
    }

    public override string GetSummary()
    {
        return "A simple example showing how to assign a material to a model";
    }

    public override string GetDescription()
    {
        return """
               At initialisation:
               
               // First load the package from your machine
               _materialPackage = MaterialPackage.Load(filePath);
               
               // Load the shader
               var shader = _materialPackage.LoadShader();
               _currentShader = shader;
               
               // Create the lights of the scene. Light properties will be sent to each referenced shader
               CreateLights([_currentShader.Value]);
               
               // Assign the shader to any model your want
               Raylib.SetMaterialShader(ref _currentModel, 0, ref shader);
               
               // Update once uniform values and texture
               var material = Raylib.GetMaterial(ref _currentModel, 0);
               _materialPackage.SendVariablesToMaterial(material);
               
               
               Then for each tick:
               
               // Send light properties to referenced shaders
               UpdateLights();
               
               // Update uniform values and texture
               var material = Raylib.GetMaterial(ref _currentModel, 0);
               _materialPackage.SendVariablesToMaterial(material);

               """;
    }

    public override void Init()
    {
        // Define our custom camera to look into our 3d world
        _camera = new Camera3D(new Vector3(5f, 5f, -5),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            45f,
            CameraProjection.Perspective);

        var mesh = Raylib.GenMeshCube(2, 2, 2);
        _currentModel = Raylib.LoadModelFromMesh(mesh);

        EnumerateMaterials();
    }


    public override void Run()
    {
        Raylib.UpdateCamera(ref _camera, CameraMode.Orbital);

        if (_currentShader.HasValue)
        {
            // Send light properties to referenced shaders
            UpdateLights();

            // Update uniform values and texture
            var material = Raylib.GetMaterial(ref _currentModel, 0);
            _materialPackage.SendVariablesToMaterial(material, false);
        }

        RenderModels();

        RenderUi();
    }

    public override void Close()
    {
        _materialPackage?.Dispose();
    }

    private void RenderUi()
    {
        ImGui.SetNextWindowPos(new Vector2(20, 100), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200, 700), ImGuiCond.Once);
        if (ImGui.Begin("Materials"))
        {
            foreach (var file in Files)
            {
                if (ImGui.Button(file.Name))
                {
                    OnMaterialSelected(file.FullName);
                }
            }
        }

        ImGui.End();
    }

    private void OnMaterialSelected(string filePath)
    {
        // First load the package from your machine
        _materialPackage = MaterialPackage.Load(filePath);

        // Load the shader
        var shader = _materialPackage.LoadShader();
        _currentShader = shader;

        // Create the lights of the scene. Light properties will be sent to each referenced shader
        CreateLights([_currentShader.Value]);

        // Assign the shader to any model your want
        Raylib.SetMaterialShader(ref _currentModel, 0, ref shader);

        // Update once uniform values and texture
        var material = Raylib.GetMaterial(ref _currentModel, 0);
        _materialPackage.SendVariablesToMaterial(material, true);
    }

    private void RenderModels()
    {
        Raylib.BeginMode3D(_camera);

        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);
        RenderLights();

        Raylib.EndMode3D();
    }
}