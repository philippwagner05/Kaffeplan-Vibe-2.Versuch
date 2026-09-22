using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class KalenderwocheTests
{
    [TestMethod]
    [DataRow(2025, 52)]
    [DataRow(2026, 53)]
    [DataRow(2027, 52)]
    [DataRow(2032, 53)]
    public void WochenImJahr_liefert_die_ISO_Wochenzahl(int jahr, int erwartet)
    {
        Assert.AreEqual(erwartet, Kalenderwoche.WochenImJahr(jahr));
    }

    [TestMethod]
    public void Der_29_12_2025_gehoert_bereits_zur_KW1_von_2026()
    {
        var woche = Kalenderwoche.VonDatum(new DateOnly(2025, 12, 29));

        Assert.AreEqual(2026, woche.Jahr);
        Assert.AreEqual(1, woche.Woche);
    }

    [TestMethod]
    public void Der_1_1_2026_liegt_in_KW1_von_2026()
    {
        var woche = Kalenderwoche.VonDatum(new DateOnly(2026, 1, 1));

        Assert.AreEqual(2026, woche.Jahr);
        Assert.AreEqual(1, woche.Woche);
    }

    [TestMethod]
    public void Montag_und_Sonntag_umschliessen_die_Woche()
    {
        var woche = new Kalenderwoche(2026, 1);

        Assert.AreEqual(new DateOnly(2025, 12, 29), woche.Montag);
        Assert.AreEqual(new DateOnly(2026, 1, 4), woche.Sonntag);
        Assert.AreEqual(DayOfWeek.Monday, woche.Montag.DayOfWeek);
        Assert.AreEqual(DayOfWeek.Sunday, woche.Sonntag.DayOfWeek);
    }

    [TestMethod]
    public void Naechste_springt_ueber_die_Jahresgrenze()
    {
        Assert.AreEqual(new Kalenderwoche(2026, 1), new Kalenderwoche(2025, 52).Naechste());
        Assert.AreEqual(new Kalenderwoche(2027, 1), new Kalenderwoche(2026, 53).Naechste());
    }

    [TestMethod]
    public void Vorherige_springt_ueber_die_Jahresgrenze()
    {
        Assert.AreEqual(new Kalenderwoche(2025, 52), new Kalenderwoche(2026, 1).Vorherige());
        Assert.AreEqual(new Kalenderwoche(2026, 53), new Kalenderwoche(2027, 1).Vorherige());
    }

    [TestMethod]
    public void Die_53_Woche_gibt_es_2026_aber_nicht_2025()
    {
        var woche = new Kalenderwoche(2026, 53);
        Assert.AreEqual(53, woche.Woche);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Kalenderwoche(2025, 53));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(54)]
    public void Ungueltige_Wochennummern_werden_abgelehnt(int woche)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Kalenderwoche(2026, woche));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(10000)]
    public void Ungueltige_Jahre_werden_abgelehnt(int jahr)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Kalenderwoche(jahr, 1));
    }

    [TestMethod]
    public void Textform_und_Parsen_passen_zusammen()
    {
        var woche = new Kalenderwoche(2026, 53);

        Assert.AreEqual("2026-W53", woche.ToString());
        Assert.AreEqual(woche, Kalenderwoche.Parse("2026-W53"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Unsinn")]
    [DataRow("2026-W54")]
    [DataRow("2025-W53")]
    [DataRow("2026W01")]
    [DataRow("2026-53")]
    public void TryParse_lehnt_ungueltige_Eingaben_ab(string text)
    {
        Assert.IsFalse(Kalenderwoche.TryParse(text, out _));
    }

    [TestMethod]
    public void Kalenderwochen_lassen_sich_sortieren()
    {
        var wochen = new List<Kalenderwoche>
        {
            new(2026, 5),
            new(2025, 52),
            new(2026, 1),
        };

        wochen.Sort();

        CollectionAssert.AreEqual(
            new List<Kalenderwoche> { new(2025, 52), new(2026, 1), new(2026, 5) },
            wochen);
    }

    [TestMethod]
    public void Jahreswochen_liefert_alle_Wochen_des_Jahres()
    {
        IReadOnlyList<Kalenderwoche> wochen = Kalenderwoche.Jahreswochen(2026);

        Assert.HasCount(53, wochen);
        Assert.AreEqual(new Kalenderwoche(2026, 1), wochen[0]);
        Assert.AreEqual(new Kalenderwoche(2026, 53), wochen[^1]);
    }
}
