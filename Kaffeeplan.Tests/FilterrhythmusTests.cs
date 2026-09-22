using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class FilterrhythmusTests
{
    private static readonly int[] ErwarteteFilterwochen = [1, 9, 17, 25, 33, 41, 49];

    [TestMethod]
    [DataRow(2025)]
    [DataRow(2026)]
    [DataRow(2027)]
    [DataRow(2032)]
    public void Die_Filterwochen_sind_1_9_17_25_33_41_49(int jahr)
    {
        IReadOnlyList<int> wochen = Filterrhythmus.Standard.Filterwochen(jahr);

        CollectionAssert.AreEqual(ErwarteteFilterwochen, wochen.ToArray());
    }

    [TestMethod]
    [DataRow(2025, 52)]
    [DataRow(2026, 53)]
    public void Auch_ein_Jahr_mit_53_Wochen_hat_genau_sieben_Filtertermine(int jahr, int erwarteteWochenzahl)
    {
        Assert.AreEqual(erwarteteWochenzahl, Kalenderwoche.WochenImJahr(jahr));
        Assert.HasCount(7, Filterrhythmus.Standard.Filterwochen(jahr));
    }

    [TestMethod]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(8, false)]
    [DataRow(9, true)]
    [DataRow(49, true)]
    [DataRow(50, false)]
    [DataRow(53, false)]
    public void IstFilterwoche_beantwortet_einzelne_Wochen(int woche, bool erwartet)
    {
        Assert.AreEqual(erwartet, Filterrhythmus.Standard.IstFilterwoche(woche));
    }

    [TestMethod]
    public void Der_Rhythmus_laesst_sich_abweichend_einstellen()
    {
        var rhythmus = new Filterrhythmus(startwoche: 3, intervall: 10);

        CollectionAssert.AreEqual(new[] { 3, 13, 23, 33, 43 }, rhythmus.Filterwochen(2025).ToArray());
    }

    [TestMethod]
    [DataRow(0, 8)]
    [DataRow(1, 0)]
    [DataRow(-1, 8)]
    public void Unsinnige_Rhythmen_werden_abgelehnt(int startwoche, int intervall)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Filterrhythmus(startwoche, intervall));
    }
}
