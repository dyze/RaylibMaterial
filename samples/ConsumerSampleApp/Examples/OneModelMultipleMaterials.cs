using ImGuiNET;
using Library.Packaging;
using Raylib_cs;
using System.Numerics;
using NLog;

namespace ConsumerSampleApp.Examples;

internal class OneModelMultipleMaterials : ExampleBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Model _currentModel;

    private Camera3D _camera;

    // key is the material index (1 or 2)
    private Dictionary<int, MaterialPackage?> _materialPackages = new()
    {
        { 1, null }, { 2, null }, {3, null}
    };

    public OneModelMultipleMaterials(Configuration.Configuration configuration)
        : base(configuration)
    {
    }

    public override string GetName()
    {
        return "One model - Multiple materials";
    }

    public override string GetSummary()
    {
        return "An example showing how to assign our materials to a model using several materials";
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
               // Please note the use of materialIndex (int). Indicate there the index of the material
               // In our example, the model coming from blender has 4 materials:
               // The first (0) seems to be default material not useful for us
               // We play then with index 1, 2 and 3 (eyes).
               Raylib.SetMaterialShader(ref _currentModel, materialIndex, ref shader);
               
               // Update once uniform values and texture
               var material = Raylib.GetMaterial(ref _currentModel, materialIndex);
               _materialPackage.SendVariablesToMaterial(material, true);

               Then for each tick:

               // Send light properties to referenced shaders
               UpdateLights();

               // Update uniform values and texture
               var material = Raylib.GetMaterial(ref _currentModel, materialIndex);
               _materialPackage.SendVariablesToMaterial(material, true);

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

        var filePath = Path.GetFullPath($"{Configuration.ModelFolderPath}/glb/monkey-3-materials.glb");

        _currentModel = Raylib.LoadModel(filePath);

        Logger.Trace($"{_currentModel.MaterialCount} materials detected");
            
        if (Raylib.IsModelValid(_currentModel) == false)
            throw new FileLoadException($"{filePath} is not valid");

        EnumerateMaterials();
    }

    public override void Run()
    {
        Raylib.UpdateCamera(ref _camera, CameraMode.Orbital);

        // Send light properties to referenced shaders
        UpdateLights();

        foreach (var (key, materialPackage) in _materialPackages)
        {
            if (materialPackage?.Shader != null)
            {
                // Update uniform values and texture
                var material = Raylib.GetMaterial(ref _currentModel, key);
                materialPackage.SendVariablesToMaterial(material, true);
            }
        }

        RenderModels();

        RenderUi();
    }

    public override void Close()
    {
        foreach (var (_, materialPackage) in _materialPackages)
            materialPackage?.Dispose();
    }

    private void RenderUi()
    {
        ImGui.SetNextWindowPos(new Vector2(20, 100), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200, 700), ImGuiCond.Once);
        if (ImGui.Begin("Materials"))
        {
            foreach (var (key, _) in _materialPackages)
            {
                ImGui.SeparatorText($"material {key}");

                ImGui.PushID($"material {key}");

                foreach (var file in Files)
                {
                    if (ImGui.Button(file.Name))
                    {
                        OnMaterialSelected(file.FullName, key);
                    }
                }

                ImGui.PopID();
            }
        }

        ImGui.End();
    }


    private void OnMaterialSelected(string filePath, int materialIndex)
    {
        // First load the package from your machine
        var materialPackage = MaterialPackage.Load(filePath);

        // Load the shader
        var shader = materialPackage.LoadShader();

        _materialPackages[materialIndex] = materialPackage;

        List<Shader> shaders = [];
        foreach (var (_, package) in _materialPackages)
        {
            if(package!= null && package.Shader != null)
                shaders.Add(package.Shader.Value);
        }

        // Create the lights of the scene. Light properties will be sent to each referenced shader
        CreateLights(shaders);

        // Assign the shader to any model your want
        Raylib.SetMaterialShader(ref _currentModel, materialIndex, ref shader);

        // Update once uniform values and texture
        var material = Raylib.GetMaterial(ref _currentModel, materialIndex);
        materialPackage.SendVariablesToMaterial(material, true);
    }

    private void RenderModels()
    {
        Raylib.BeginMode3D(_camera);

        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);
        RenderLights();

        Raylib.EndMode3D();
    }
}