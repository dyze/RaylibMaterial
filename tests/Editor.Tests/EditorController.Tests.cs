namespace Editor.Tests;

[TestClass]
public class EditorControllerTest
{
    [TestMethod]
    public void NewMaterial()
    {
        EditorController controller = new(null);
        controller.InitUi();

        controller.NewMaterial();
    }

    [TestMethod]
    public void LoadInexistentMaterial()
    {
        EditorController controller = new(null);
        controller.InitUi();

        Assert.AreEqual(false, controller.LoadMaterial("do-not-exist.mat"));
    }

    [TestMethod]
    public void LoadExistentMaterial()
    {
        EditorController controller = new(null);
        controller.InitUi();

        Assert.AreEqual(false, controller.LoadMaterial("./materials/textured.mat"));
    }
}   