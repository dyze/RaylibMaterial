using Library.CodeVariable;
using Library.Packaging;

namespace Library.Tests;

[TestClass]
public sealed class MaterialMetaFileTest
{
    [TestMethod]
    public void SaveLoad()
    {
        var material = new MaterialMetaFile();
        material.Author = "Author";
        material.Description = "Description";
        material.Tags.Add("materialMeta");
        material.Tags.Add("textured");

        material.Variables.Add("color", new CodeVariableVector4());
        material.Variables.Add("texture", new CodeVariableTexture());

        MaterialMetaFileStorage.Save(material, "MaterialMetaTest/test.mat");

        var loadedMaterial = MaterialMetaFileStorage.Load("MaterialMetaTest/test.mat");
        Assert.IsNotNull(loadedMaterial != null);

        if (loadedMaterial == null)
            throw new NullReferenceException("loadedMaterial is null");

        Assert.AreEqual(loadedMaterial.Tags.Count, material.Tags.Count);
        Assert.AreEqual(loadedMaterial.Tags[0], material.Tags[0]);
        Assert.AreEqual(loadedMaterial.Tags[1], material.Tags[1]);

        Assert.AreEqual(loadedMaterial.Variables.Count, material.Variables.Count);
        Assert.AreEqual(loadedMaterial.Variables["color"].GetType(), material.Variables["color"].GetType());
        //Assert.AreEqual(loadedMaterial.Variables["color"].Value, material.Variables["color"].Value);
        Assert.AreEqual(loadedMaterial.Variables["texture"].GetType(), material.Variables["texture"].GetType());
        //Assert.AreEqual(loadedMaterial.Variables["texture"].Value, material.Variables["texture"].Value);
    }
}