using ImGuiNET;
using Library.Packaging;
using Raylib_cs;
using System.Numerics;

namespace ConsumerSampleApp;

internal class OneModelTwoMaterials : ExampleBase
{
    private Model _currentModel;

    private Camera3D _camera;
    private FileInfo[] _files = [];
    private MaterialPackage? _materialPackage;



    private int _materialIndex = 1;

    public override void Init()
    {
        // Define our custom camera to look into our 3d world
        _camera = new Camera3D(new Vector3(5f, 5f, -5),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            45f,
            CameraProjection.Perspective);

        _currentModel = Raylib.LoadModel($"{_modelDirectoryPath}/monkey-2-materials.glb");

        EnumerateMaterials();
    }

    private void EnumerateMaterials()
    {
        var di = new DirectoryInfo(_materialDirectoryPath);

        _files = di.GetFiles("*.mat", SearchOption.AllDirectories);
    }

    public override void Run()
    {
        Raylib.UpdateCamera(ref _camera, CameraMode.Orbital);

        if (CurrentShader.HasValue)
            UpdateLights();

        Raylib.ClearBackground(Color.Black);

        RenderModels();

        RenderUi();
    }

    public override void Close()
    {
        _materialPackage?.Dispose();
    }

    private void RenderUi()
    {
        ImGui.SetNextWindowSize(new Vector2(200, 400));
        if (ImGui.Begin("Materials"))
        {
            foreach (var file in _files)
            {
                if (ImGui.Button(file.Name))
                {
                    OnMaterialSelected(file.FullName);
                }
            }

            ImGui.End();
        }
    }

    private void OnMaterialSelected(string filePath)
    {
        _materialPackage = MaterialPackage.Load(filePath);

        var shader = _materialPackage.LoadShader();
        CurrentShader = shader;
        Raylib.SetMaterialShader(ref _currentModel, _materialIndex, ref shader);

        var material = Raylib.GetMaterial(ref _currentModel, _materialIndex);
        _materialPackage.SendVariablesToModel(material, true);

        CreateLights();
    }

    private void RenderModels()
    {
        Raylib.BeginMode3D(_camera);

        Raylib.DrawModel(_currentModel, Vector3.Zero, 1f, Color.White);
        RenderLights();

        Raylib.EndMode3D();
    }


}