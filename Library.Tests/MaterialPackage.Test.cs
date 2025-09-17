using Library.Packaging;

namespace Library.Tests;

[TestClass]
public sealed class MaterialPackageTest
{
    [TestMethod]
    public void SaveLoad()
    {
        var materialPackage = new MaterialPackage();
        materialPackage.Meta.Author = "author";
        materialPackage.AddFile("image1.png", [1, 2, 3]);
        materialPackage.AddFile("shader1.frag", [4, 5, 6]);

        materialPackage.Save("MaterialPackageTest/test.mat");

        materialPackage = null;

        materialPackage = new MaterialPackage();

        materialPackage.Load("MaterialPackageTest/test.mat");

        Assert.AreEqual("author", materialPackage.Meta.Author);
        Assert.AreEqual(2, materialPackage.Files.Count);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, materialPackage.Files[new FileId(FileType.Image, "image1.png")]);
        CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, materialPackage.Files[new FileId(FileType.FragmentShader, "shader1.frag")]);
    }
}