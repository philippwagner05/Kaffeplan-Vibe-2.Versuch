using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class MitarbeiterTests
{
    [TestMethod]
    public void Gleiche_Id_und_gleicher_Name_bedeuten_gleich()
    {
        var links = new Mitarbeiter(Testdaten.Id(0), "Ralf");
        var rechts = new Mitarbeiter(Testdaten.Id(0), "Ralf");

        Assert.AreEqual(links, rechts);
        Assert.AreEqual(links.GetHashCode(), rechts.GetHashCode());
    }

    [TestMethod]
    public void Verschiedene_Ids_bedeuten_verschieden()
    {
        Assert.AreNotEqual(
            new Mitarbeiter(Testdaten.Id(0), "Ralf"),
            new Mitarbeiter(Testdaten.Id(1), "Ralf"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ein_leerer_Name_wird_abgelehnt(string name)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Mitarbeiter(Testdaten.Id(0), name));
    }

    [TestMethod]
    public void Eine_leere_Id_wird_abgelehnt()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Mitarbeiter(Guid.Empty, "Ralf"));
    }

    [TestMethod]
    public void Umgebende_Leerzeichen_werden_entfernt()
    {
        Assert.AreEqual("Ralf", new Mitarbeiter("  Ralf  ").Name);
    }

    [TestMethod]
    public void Ohne_Id_wird_eine_neue_vergeben()
    {
        var person = new Mitarbeiter("Ralf");

        Assert.AreNotEqual(Guid.Empty, person.Id);
    }
}
