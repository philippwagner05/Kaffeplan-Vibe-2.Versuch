using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class StatistikrechnerTests
{
    [TestMethod]
    [DataRow(2025, 52)]
    [DataRow(2026, 53)]
    public void Die_Reinigungen_summieren_sich_auf_die_Wochenzahl(int jahr, int wochen)
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(jahr, team).Plan!;

        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(plan, team);

        Assert.AreEqual(wochen, statistik.Sum(s => s.Reinigungen));
        Assert.AreEqual(7, statistik.Sum(s => s.Filtertausche));
    }

    [TestMethod]
    public void Die_Statistik_folgt_der_Reihenfolge_der_Mitarbeiterliste()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(plan, team);

        CollectionAssert.AreEqual(
            team.Select(m => m.Name).ToArray(),
            statistik.Select(s => s.Mitarbeiter.Name).ToArray());
    }

    [TestMethod]
    public void Eine_Person_ohne_Einsatz_erscheint_mit_null()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        // Nur die ersten beiden Personen werden eingeteilt.
        Dienstplan plan = Testdaten.PlanAus(2025, team, woche => woche % 2);

        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(plan, team);

        Assert.HasCount(6, statistik);
        Assert.AreEqual(4, statistik.Count(s => s.Reinigungen == 0));
    }

    [TestMethod]
    public void Ein_Filtertausch_zaehlt_zusaetzlich_zur_Reinigung_derselben_Person()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2025, team).Plan!;

        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(plan, team);

        // Wer den Filter tauscht, reinigt in derselben Woche auch - es gibt keine
        // Filterwoche ohne zugehoerige Reinigung.
        Assert.IsTrue(statistik.All(s => s.Filtertausche <= s.Reinigungen));
    }
}
