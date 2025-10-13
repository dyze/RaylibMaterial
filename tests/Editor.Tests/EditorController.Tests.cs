using Editor.EditorControllerNS;

namespace Editor.Tests;

public static class LockClass
{
    public static object LockObject = new();
}

[TestClass]
public class EditorControllerTest
{
    // Tests of this class must be executed sequentially
    [TestInitialize]
    public void TestSetup()
    {
        Monitor.Enter(LockClass.LockObject);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Monitor.Exit(LockClass.LockObject);
    }

    [TestMethod]
    public void InitClose()
    {
        EditorController controller = new(null);
        controller.Init();

        controller.Close();
    }

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

        Assert.AreEqual(true, controller.LoadMaterial("./materials/textured.mat"));

        controller.SaveAs("./materials/textured-save-as.mat", false);

        Assert.IsTrue(File.Exists("./materials/textured-save-as.mat"));

        // reopen
        Assert.AreEqual(true, controller.LoadMaterial("./materials/textured-save-as.mat"));
    }
}   