using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

/// <summary>
/// Der Pruefer muss zuerst selbst geprueft sein - sonst wird spaeter der Planer mit
/// einem Werkzeug bewertet, von dem niemand weiss, ob es Verstoesse ueberhaupt sieht.
/// </summary>
[TestClass]
public sealed class RegelprueferTests
{
    [TestMethod]
    public void Naive_Rotation_bei_sechs_Personen_erfuellt_F1_und_F3_aber_verletzt_F2()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = Testdaten.PlanAus(2025, team, Testdaten.NaiveRotation(6));

        Pruefergebnis ergebnis = Regelpruefer.Pruefe(plan, team);

        Assert.IsTrue(ergebnis.F1Erfuellt, "Reihum verteilt die Reinigungen gleichmaessig.");
        Assert.IsTrue(ergebnis.F3Erfuellt, "Reihum wiederholt niemanden in der Folgewoche.");
        Assert.IsFalse(ergebnis.F2Erfuellt, "Genau hier faellt stures Reihum durch.");
        Assert.AreEqual(3, ergebnis.FilterSpanne);
    }

    [TestMethod]
    public void Naive_Rotation_bei_acht_Personen_haeuft_alle_Filtertausche_auf_einer_Person()
    {
        List<Mitarbeiter> team = Testdaten.Team(8);
        Dienstplan plan = Testdaten.PlanAus(2025, team, Testdaten.NaiveRotation(8));

        Pruefergebnis ergebnis = Regelpruefer.Pruefe(plan, team);

        // Acht Personen bei Achtwochenrhythmus: alle sieben Filterwochen treffen dieselbe Person.
        Assert.IsFalse(ergebnis.F2Erfuellt);
        Assert.AreEqual(7, ergebnis.FilterSpanne);

        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(plan, team);
        Assert.AreEqual(7, statistik[0].Filtertausche);
        Assert.IsTrue(statistik.Skip(1).All(s => s.Filtertausche == 0));
    }

    [TestMethod]
    public void Ein_Plan_der_nur_zwei_von_sechs_Personen_einteilt_verletzt_F1()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = Testdaten.PlanAus(2025, team, woche => woche % 2);

        Pruefergebnis ergebnis = Regelpruefer.Pruefe(plan, team);

        Assert.IsFalse(ergebnis.F1Erfuellt);
        Assert.AreEqual(26, ergebnis.ReinigungsSpanne);
        Assert.IsTrue(ergebnis.Verstoesse.Any(v => v.Regel == Regelpruefer.F1));
    }

    [TestMethod]
    public void Ein_Plan_mit_zwei_gleichen_Wochen_hintereinander_verletzt_F3()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        // Jede Person zwei Wochen am Stueck.
        Dienstplan plan = Testdaten.PlanAus(2025, team, woche => (woche - 1) / 2 % 6);

        Pruefergebnis ergebnis = Regelpruefer.Pruefe(plan, team);

        Assert.IsFalse(ergebnis.F3Erfuellt);
        Assert.AreEqual(26, ergebnis.Verstoesse.Count(v => v.Regel == Regelpruefer.F3));
    }

    [TestMethod]
    public void Ein_regelkonformer_Plan_meldet_keine_Verstoesse()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2025, team);

        Pruefergebnis geprueft = Regelpruefer.Pruefe(ergebnis.Plan!, team);

        Assert.IsTrue(geprueft.AllesErfuellt, geprueft.ToString());
        Assert.IsEmpty(geprueft.Verstoesse);
    }

    [TestMethod]
    public void Ohne_Mitarbeiter_laesst_sich_nichts_pruefen()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = Testdaten.PlanAus(2025, team, Testdaten.NaiveRotation(6));

        Assert.ThrowsExactly<ArgumentException>(() => Regelpruefer.Pruefe(plan, []));
    }
}
