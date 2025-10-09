using Editor.EditorControllerNS;

namespace Editor.Tests;

[TestClass]
public class EditorControllerTest
{
    [TestMethod]
    public void NewMaterial()
    {
        EditorController controller = new(null);
        controller.Init();

        controller.NewMaterial();
    }

    [TestMethod]
    public void LoadInexistentMaterial()
    {
        EditorController controller = new(null);
        controller.Init();

        Assert.AreEqual(false, controller.LoadMaterial("do-not-exist.mat"));
    }

    [TestMethod]
    public void LoadExistentMaterial()
    {
        EditorController controller = new(null);
        controller.Init();

        Assert.AreEqual(true, controller.LoadMaterial("./materials/textured.mat"));
    }

    [TestMethod]
    public void SaveAs()
    {
        EditorController controller = new(null);
        controller.Init();

        Assert.AreEqual(false, controller.LoadMaterial("./materials/textured.mat"));

        controller.SaveAs("./materials/textured-save-as.mat", false);
    }
}   