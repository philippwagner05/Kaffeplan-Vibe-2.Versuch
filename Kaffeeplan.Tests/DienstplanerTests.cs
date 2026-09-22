using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class DienstplanerTests
{
    // ----- F1, F2 und F3 ueber die Bandbreite an Teamgroessen und Jahren -----

    [TestMethod]
    [DataRow(2025, 3)]
    [DataRow(2025, 4)]
    [DataRow(2025, 5)]
    [DataRow(2025, 6)]
    [DataRow(2025, 7)]
    [DataRow(2025, 8)]
    [DataRow(2026, 3)]
    [DataRow(2026, 4)]
    [DataRow(2026, 5)]
    [DataRow(2026, 6)]
    [DataRow(2026, 7)]
    [DataRow(2026, 8)]
    [DataRow(2027, 3)]
    [DataRow(2027, 4)]
    [DataRow(2027, 5)]
    [DataRow(2027, 6)]
    [DataRow(2027, 7)]
    [DataRow(2027, 8)]
    [DataRow(2032, 3)]
    [DataRow(2032, 4)]
    [DataRow(2032, 5)]
    [DataRow(2032, 6)]
    [DataRow(2032, 7)]
    [DataRow(2032, 8)]
    public void Der_erzeugte_Plan_erfuellt_F1_F2_und_F3(int jahr, int anzahlPersonen)
    {
        List<Mitarbeiter> team = Testdaten.Team(anzahlPersonen);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(jahr, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        Pruefergebnis geprueft = Regelpruefer.Pruefe(ergebnis.Plan!, team);
        Assert.IsTrue(geprueft.AllesErfuellt, $"{jahr} mit {anzahlPersonen} Personen: {geprueft}");
    }

    // ----- Der ausdrueckliche Fall: 8 Mitarbeiter bei 8-Wochen-Rhythmus -----

    [TestMethod]
    [DataRow(2025)]
    [DataRow(2026)]
    public void Acht_Mitarbeiter_im_Achtwochenrhythmus_bekommen_die_Filtertausche_gleichmaessig(int jahr)
    {
        List<Mitarbeiter> team = Testdaten.Team(8);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(jahr, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        Pruefergebnis geprueft = Regelpruefer.Pruefe(ergebnis.Plan!, team);
        Assert.IsTrue(geprueft.AllesErfuellt, geprueft.ToString());

        // Genau der Fall, an dem stures Reihum scheitert: Teamgroesse gleich Filterintervall.
        // Sieben Termine auf acht Personen heisst sieben mal 1 und einmal 0.
        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(ergebnis.Plan!, team);
        CollectionAssert.AreEquivalent(
            new[] { 1, 1, 1, 1, 1, 1, 1, 0 },
            statistik.Select(s => s.Filtertausche).ToArray());

        // Jede Filterwoche trifft eine andere Person.
        var personenDerFilterwochen = ergebnis.Plan!.Filterwochen.Select(e => e.MitarbeiterId).ToList();
        Assert.AreEqual(7, personenDerFilterwochen.Distinct().Count());
    }

    // ----- Der ausdrueckliche Fall: Jahr 2026 mit 53 Wochen -----

    [TestMethod]
    public void Das_Jahr_2026_wird_mit_53_Kalenderwochen_geplant()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        Assert.AreEqual(53, ergebnis.Plan!.Wochenanzahl);
        CollectionAssert.AreEqual(
            Enumerable.Range(1, 53).ToArray(),
            ergebnis.Plan.Eintraege.Select(e => e.Woche.Woche).ToArray());

        Pruefergebnis geprueft = Regelpruefer.Pruefe(ergebnis.Plan, team);
        Assert.IsTrue(geprueft.AllesErfuellt, geprueft.ToString());

        // 53 Wochen auf 6 Personen: fuenf reinigen 9 mal, eine 8 mal.
        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(ergebnis.Plan, team);
        Assert.AreEqual(5, statistik.Count(s => s.Reinigungen == 9));
        Assert.AreEqual(1, statistik.Count(s => s.Reinigungen == 8));
    }

    // ----- Der ausdrueckliche Fall: 0 Mitarbeiter -----

    [TestMethod]
    public void Ohne_Mitarbeiter_kommt_eine_Meldung_statt_eines_Absturzes()
    {
        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, []);

        Assert.IsFalse(ergebnis.Erfolgreich);
        Assert.IsNull(ergebnis.Plan);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ergebnis.Fehlermeldung));
    }

    [TestMethod]
    public void Eine_null_Liste_kommt_ebenfalls_als_Meldung_zurueck()
    {
        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, null!);

        Assert.IsFalse(ergebnis.Erfolgreich);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ergebnis.Fehlermeldung));
    }

    // ----- Weitere Grenzfaelle (A11) -----

    [TestMethod]
    public void Mit_einer_Person_ist_F3_unmoeglich_und_das_wird_gemeldet()
    {
        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, Testdaten.Team(1));

        Assert.IsFalse(ergebnis.Erfolgreich);
        StringAssert.Contains(ergebnis.Fehlermeldung!, "F1, F2 und F3");
    }

    [TestMethod]
    public void Mit_zwei_Personen_schliessen_sich_F2_und_F3_aus_und_das_wird_gemeldet()
    {
        // F3 erzwingt strikte Abwechslung. Alle Filterwochen sind ungerade und treffen
        // damit zwangslaeufig dieselbe Person - F2 waere verletzt. Es gibt keinen Plan.
        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, Testdaten.Team(2));

        Assert.IsFalse(ergebnis.Erfolgreich);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ergebnis.Fehlermeldung));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    [DataRow(9999)]
    [DataRow(10000)]
    public void Ein_ungueltiges_Jahr_kommt_als_Meldung_zurueck(int jahr)
    {
        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(jahr, Testdaten.Team(6));

        Assert.IsFalse(ergebnis.Erfolgreich);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ergebnis.Fehlermeldung));
    }

    [TestMethod]
    public void Das_letzte_gueltige_Jahr_laesst_sich_vollstaendig_planen()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(Kalenderwoche.MaxJahr, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        Assert.IsTrue(Regelpruefer.Pruefe(ergebnis.Plan!, team).AllesErfuellt);

        // Der Plan muss sich auch anzeigen und exportieren lassen - genau daran
        // scheiterte frueher das Jahr 9999.
        foreach (Wocheneintrag eintrag in ergebnis.Plan!.Eintraege)
        {
            Assert.AreEqual(DayOfWeek.Sunday, eintrag.Woche.Sonntag.DayOfWeek);
        }
    }

    // ----- Grosse Teams -----
    // Wer eine Filterwoche zugewiesen bekommt, braucht dafuer noch Kontingent. Ohne
    // diese Reservierung verbrauchte die gierige Auswahl das Kontingent vorher an
    // freien Wochen; die Suche lief dann in eine Sackgasse und meldete ab etwa 18
    // Personen "kein Plan", obwohl einer existiert.

    [TestMethod]
    [DataRow(2025, 18)]
    [DataRow(2025, 19)]
    [DataRow(2025, 26)]
    [DataRow(2025, 40)]
    [DataRow(2025, 52)]
    [DataRow(2026, 18)]
    [DataRow(2026, 20)]
    [DataRow(2026, 53)]
    public void Auch_grosse_Teams_bekommen_einen_gueltigen_Plan(int jahr, int anzahlPersonen)
    {
        List<Mitarbeiter> team = Testdaten.Team(anzahlPersonen);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(jahr, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        Pruefergebnis geprueft = Regelpruefer.Pruefe(ergebnis.Plan!, team);
        Assert.IsTrue(geprueft.AllesErfuellt, $"{jahr} mit {anzahlPersonen} Personen: {geprueft}");
    }

    [TestMethod]
    public void Fuer_jede_Teamgroesse_ab_drei_Personen_wird_ein_Plan_gefunden()
    {
        // Ein Rundumschlag statt einzelner Stichproben: ab drei Personen gibt es fuer
        // jede Groesse bis zur Wochenzahl einen Plan, und keine Groesse laesst die
        // Suche in die Abbruchgrenze laufen.
        foreach (int jahr in new[] { 2025, 2026 })
        {
            for (int anzahl = 3; anzahl <= Kalenderwoche.WochenImJahr(jahr); anzahl++)
            {
                List<Mitarbeiter> team = Testdaten.Team(anzahl);

                PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(jahr, team);

                Assert.IsTrue(ergebnis.Erfolgreich, $"{jahr} mit {anzahl} Personen: {ergebnis.Fehlermeldung}");
                Pruefergebnis geprueft = Regelpruefer.Pruefe(ergebnis.Plan!, team);
                Assert.IsTrue(geprueft.AllesErfuellt, $"{jahr} mit {anzahl} Personen: {geprueft}");
            }
        }
    }

    [TestMethod]
    public void Dieselbe_Person_doppelt_in_der_Liste_wird_gemeldet()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        team.Add(team[0]);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, team);

        Assert.IsFalse(ergebnis.Erfolgreich);
        Assert.IsFalse(string.IsNullOrWhiteSpace(ergebnis.Fehlermeldung));
    }

    // ----- Die in der Aufgabenstellung ausgerechnete Verteilung -----

    [TestMethod]
    public void Sechs_Personen_und_52_Wochen_ergeben_die_in_der_Spezifikation_genannte_Verteilung()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2025, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        Assert.AreEqual(52, ergebnis.Plan!.Wochenanzahl);

        IReadOnlyList<Mitarbeiterstatistik> statistik = Statistikrechner.Berechne(ergebnis.Plan, team);

        // Vier Personen reinigen 9 mal, zwei Personen 8 mal: 4 * 9 + 2 * 8 = 52.
        Assert.AreEqual(4, statistik.Count(s => s.Reinigungen == 9));
        Assert.AreEqual(2, statistik.Count(s => s.Reinigungen == 8));

        // Eine Person hat 2 Filtertermine, fuenf haben je einen: 1 * 2 + 5 * 1 = 7.
        Assert.AreEqual(1, statistik.Count(s => s.Filtertausche == 2));
        Assert.AreEqual(5, statistik.Count(s => s.Filtertausche == 1));
    }

    // ----- Struktur des Plans -----

    [TestMethod]
    [DataRow(2025)]
    [DataRow(2026)]
    public void Die_Filterwochen_sind_im_Plan_markiert(int jahr)
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(jahr, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        CollectionAssert.AreEqual(
            new[] { 1, 9, 17, 25, 33, 41, 49 },
            ergebnis.Plan!.Filterwochen.Select(e => e.Woche.Woche).ToArray());
    }

    [TestMethod]
    public void Jede_Woche_ist_genau_einer_Person_aus_dem_Team_zugeordnet()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        PlanungsErgebnis ergebnis = new Dienstplaner().Erzeuge(2026, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        var bekannteIds = team.Select(m => m.Id).ToHashSet();
        Assert.IsTrue(ergebnis.Plan!.Eintraege.All(e => bekannteIds.Contains(e.MitarbeiterId)));
    }

    [TestMethod]
    public void Derselbe_Input_ergibt_immer_denselben_Plan()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        var planer = new Dienstplaner();

        Dienstplan ersterLauf = planer.Erzeuge(2026, team).Plan!;
        Dienstplan zweiterLauf = planer.Erzeuge(2026, team).Plan!;

        CollectionAssert.AreEqual(
            ersterLauf.Eintraege.Select(e => e.MitarbeiterId).ToArray(),
            zweiterLauf.Eintraege.Select(e => e.MitarbeiterId).ToArray());
    }

    [TestMethod]
    public void Ein_abweichender_Filterrhythmus_wird_beruecksichtigt()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        var planer = new Dienstplaner(new Filterrhythmus(startwoche: 2, intervall: 13));

        PlanungsErgebnis ergebnis = planer.Erzeuge(2025, team);

        Assert.IsTrue(ergebnis.Erfolgreich, ergebnis.Fehlermeldung);
        CollectionAssert.AreEqual(
            new[] { 2, 15, 28, 41 },
            ergebnis.Plan!.Filterwochen.Select(e => e.Woche.Woche).ToArray());
        Assert.IsTrue(Regelpruefer.Pruefe(ergebnis.Plan, team).AllesErfuellt);
    }
}
