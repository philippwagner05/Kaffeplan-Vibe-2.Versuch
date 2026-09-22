using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class DienstplanTests
{
    [TestMethod]
    [DataRow(2025, 52)]
    [DataRow(2026, 53)]
    public void Ein_vollstaendiger_Plan_deckt_jede_Kalenderwoche_genau_einmal_ab(int jahr, int erwarteteWochen)
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        Dienstplan plan = Testdaten.PlanAus(jahr, team, Testdaten.NaiveRotation(6));

        Assert.AreEqual(erwarteteWochen, plan.Wochenanzahl);
        CollectionAssert.AreEqual(
            Enumerable.Range(1, erwarteteWochen).ToArray(),
            plan.Eintraege.Select(e => e.Woche.Woche).ToArray());
    }

    [TestMethod]
    public void Eine_fehlende_Woche_wird_abgelehnt()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        List<Wocheneintrag> eintraege = Testdaten.PlanAus(2025, team, Testdaten.NaiveRotation(6))
            .Eintraege.Take(51).ToList();

        Assert.ThrowsExactly<ArgumentException>(() => new Dienstplan(2025, eintraege));
    }

    [TestMethod]
    public void Eine_doppelt_belegte_Woche_wird_abgelehnt()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        List<Wocheneintrag> eintraege = Testdaten.PlanAus(2025, team, Testdaten.NaiveRotation(6))
            .Eintraege.ToList();

        // KW 52 durch eine zweite KW 1 ersetzen: gleiche Anzahl, aber eine Luecke und eine Dublette.
        eintraege[51] = new Wocheneintrag(new Kalenderwoche(2025, 1), team[0].Id, true);

        Assert.ThrowsExactly<ArgumentException>(() => new Dienstplan(2025, eintraege));
    }

    [TestMethod]
    public void Ein_Eintrag_aus_dem_falschen_Jahr_wird_abgelehnt()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        List<Wocheneintrag> eintraege = Testdaten.PlanAus(2025, team, Testdaten.NaiveRotation(6))
            .Eintraege.ToList();

        eintraege[10] = new Wocheneintrag(new Kalenderwoche(2026, 11), team[0].Id, false);

        Assert.ThrowsExactly<ArgumentException>(() => new Dienstplan(2025, eintraege));
    }

    [TestMethod]
    public void Ein_Plan_mit_zu_vielen_Eintraegen_wird_abgelehnt()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        List<Wocheneintrag> eintraege = Testdaten.PlanAus(2026, team, Testdaten.NaiveRotation(6))
            .Eintraege.ToList();

        Assert.ThrowsExactly<ArgumentException>(() => new Dienstplan(2025, eintraege));
    }

    [TestMethod]
    public void Der_Indexer_liefert_die_Woche_mit_dieser_Nummer()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = Testdaten.PlanAus(2026, team, Testdaten.NaiveRotation(6));

        Assert.AreEqual(1, plan[1].Woche.Woche);
        Assert.AreEqual(53, plan[53].Woche.Woche);
        Assert.AreEqual(plan[17], plan.Eintrag(new Kalenderwoche(2026, 17)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => plan[54]);
    }

    [TestMethod]
    public void Der_Plan_kennt_seine_Filterwochen()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = Testdaten.PlanAus(2026, team, Testdaten.NaiveRotation(6));

        CollectionAssert.AreEqual(
            new[] { 1, 9, 17, 25, 33, 41, 49 },
            plan.Filterwochen.Select(e => e.Woche.Woche).ToArray());
    }
}
