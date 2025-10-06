using Library.Packaging;

namespace Library.Tests;

[TestClass]
public sealed class MaterialPackageTest
{
    [TestMethod]
    public void SaveLoad()
    {
        var materialPackage = new MaterialPackage();
        materialPackage.Description.Author = "author";
        materialPackage.AddFile("image1.png", [1, 2, 3]);
        materialPackage.AddFile("shader1.frag", [4, 5, 6]);

        materialPackage.Save("MaterialPackageTest/test.mat");

        materialPackage = MaterialPackage.Load("MaterialPackageTest/test.mat");

        Assert.AreEqual("author", materialPackage.Description.Author);
        Assert.AreEqual(2, materialPackage.Files.Count);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, materialPackage.Files[new FileId(FileType.Image, "image1.png")]);
        CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, materialPackage.Files[new FileId(FileType.FragmentShader, "shader1.frag")]);
    }

    [TestMethod]
    public void LoadExistingV1File()
    {
        var materialPackage = MaterialPackage.Load("resources/textured.mat");

        Assert.AreEqual(1, materialPackage.Description.Version);
        Assert.AreEqual("dyze", materialPackage.Description.Author);
        Assert.AreEqual(2, materialPackage.Files.Count);
        CollectionAssert.IsSubsetOf(new byte[] { 35, 118, 101 }, materialPackage.Files[new FileId(FileType.FragmentShader, "texture.frag")]);
        CollectionAssert.IsSubsetOf(new byte[] { 137, 80, 78 }, materialPackage.Files[new FileId(FileType.Image, "test.png")]);
    }
}