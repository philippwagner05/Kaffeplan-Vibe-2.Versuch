using System.ComponentModel;
using Kaffeeplan.App.ViewModels;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class MainViewModelTests
{
    private SpeicherDoppel _speicher = null!;
    private DialogDoppel _dialog = null!;
    private CsvExportDoppel _csv = null!;

    [TestInitialize]
    public void Vorbereiten()
    {
        _speicher = new SpeicherDoppel();
        _dialog = new DialogDoppel();
        _csv = new CsvExportDoppel();
    }

    private MainViewModel ViewModel(int anzahlPersonen = 6, int heutigesJahr = 2026) =>
        new(new Dienstplaner(),
            _speicher,
            _dialog,
            _csv,
            new FesteUhr(heutigesJahr),
            Testdaten.Team(anzahlPersonen));

    // ----- Jahreswahl -----

    [TestMethod]
    public void Das_aktuelle_Jahr_ist_vorbelegt()
    {
        Assert.AreEqual("2026", ViewModel(heutigesJahr: 2026).JahrEingabe);
    }

    [TestMethod]
    public void Die_Pfeiltasten_verschieben_das_Jahr()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);

        vm.JahrVorCommand.Execute(null);
        Assert.AreEqual("2027", vm.JahrEingabe);

        vm.JahrZurueckCommand.Execute(null);
        vm.JahrZurueckCommand.Execute(null);
        Assert.AreEqual("2025", vm.JahrEingabe);
    }

    [TestMethod]
    public void Ein_Jahreswechsel_rechnet_einen_bestehenden_Plan_neu()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2025);
        vm.PlanErzeugenCommand.Execute(null);
        Assert.HasCount(52, vm.Wochen);

        vm.JahrVorCommand.Execute(null);

        Assert.AreEqual("2026", vm.JahrEingabe);
        Assert.HasCount(53, vm.Wochen);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("zweitausend")]
    [DataRow("20x6")]
    [DataRow("0")]
    [DataRow("99999")]
    public void Eine_unsinnige_Jahreszahl_erzeugt_eine_Meldung_statt_eines_Absturzes(string eingabe)
    {
        MainViewModel vm = ViewModel();
        vm.JahrEingabe = eingabe;

        vm.PlanErzeugenCommand.Execute(null);

        Assert.IsFalse(vm.HatPlan);
        Assert.IsEmpty(vm.Wochen);
        StringAssert.Contains(vm.StatusMeldung, "Jahreszahl");
    }

    // ----- Plan und Anzeige -----

    [TestMethod]
    public void Der_erzeugte_Plan_fuellt_Wochenliste_und_Statistik()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);

        vm.PlanErzeugenCommand.Execute(null);

        Assert.IsTrue(vm.HatPlan);
        Assert.HasCount(53, vm.Wochen);
        Assert.HasCount(6, vm.Statistik);
        Assert.AreEqual(53, vm.Statistik.Sum(s => s.Reinigungen));
        Assert.AreEqual(7, vm.Statistik.Sum(s => s.Filtertausche));
    }

    [TestMethod]
    public void Genau_die_Filterwochen_sind_zur_Hervorhebung_markiert()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);

        vm.PlanErzeugenCommand.Execute(null);

        CollectionAssert.AreEqual(
            new[] { 1, 9, 17, 25, 33, 41, 49 },
            vm.Wochen.Where(w => w.IstFilterwoche).Select(w => w.Kalenderwoche).ToArray());
        Assert.AreEqual("Reinigung + Filtertausch", vm.Wochen[0].Aufgabe);
        Assert.AreEqual("Reinigung", vm.Wochen[1].Aufgabe);
    }

    [TestMethod]
    public void Die_Wochenzeile_zeigt_den_Zeitraum_der_Kalenderwoche()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);

        vm.PlanErzeugenCommand.Execute(null);

        Assert.AreEqual("29.12.2025 - 04.01.2026", vm.Wochen[0].Zeitraum);
    }

    [TestMethod]
    public void Der_Regelstatus_meldet_die_erfuellten_Kriterien()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);

        vm.PlanErzeugenCommand.Execute(null);

        StringAssert.Contains(vm.Regelstatus, "F1, F2 und F3 erfüllt");
    }

    [TestMethod]
    public void Ein_unloesbares_Team_liefert_eine_Meldung_und_keinen_Plan()
    {
        MainViewModel vm = ViewModel(anzahlPersonen: 2);

        vm.PlanErzeugenCommand.Execute(null);

        Assert.IsFalse(vm.HatPlan);
        Assert.IsEmpty(vm.Wochen);
        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.StatusMeldung));
    }

    // ----- Mitarbeiterpflege (A8) -----

    [TestMethod]
    public void Ein_Mitarbeiter_laesst_sich_hinzufuegen()
    {
        MainViewModel vm = ViewModel();
        vm.NeuerMitarbeiterName = "Philipp";

        vm.MitarbeiterHinzufuegenCommand.Execute(null);

        Assert.HasCount(7, vm.Mitarbeiter);
        Assert.AreEqual("Philipp", vm.Mitarbeiter[^1].Name);
        Assert.AreEqual(string.Empty, vm.NeuerMitarbeiterName);
    }

    [TestMethod]
    public void Ohne_Namen_ist_das_Hinzufuegen_gesperrt()
    {
        MainViewModel vm = ViewModel();

        Assert.IsFalse(vm.MitarbeiterHinzufuegenCommand.CanExecute(null));

        vm.NeuerMitarbeiterName = "Philipp";
        Assert.IsTrue(vm.MitarbeiterHinzufuegenCommand.CanExecute(null));

        vm.NeuerMitarbeiterName = "   ";
        Assert.IsFalse(vm.MitarbeiterHinzufuegenCommand.CanExecute(null));
    }

    [TestMethod]
    public void Ein_Mitarbeiter_laesst_sich_entfernen()
    {
        MainViewModel vm = ViewModel();
        vm.AusgewaehlterMitarbeiter = vm.Mitarbeiter[2];

        vm.MitarbeiterEntfernenCommand.Execute(null);

        Assert.HasCount(5, vm.Mitarbeiter);
        Assert.IsFalse(vm.Mitarbeiter.Any(m => m.Name == "Mario"));
        Assert.IsNull(vm.AusgewaehlterMitarbeiter);
    }

    [TestMethod]
    public void Ohne_Auswahl_ist_das_Entfernen_gesperrt()
    {
        MainViewModel vm = ViewModel();

        Assert.IsFalse(vm.MitarbeiterEntfernenCommand.CanExecute(null));

        vm.AusgewaehlterMitarbeiter = vm.Mitarbeiter[0];
        Assert.IsTrue(vm.MitarbeiterEntfernenCommand.CanExecute(null));
    }

    [TestMethod]
    public void Eine_Aenderung_am_Team_rechnet_den_Plan_neu()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);
        vm.PlanErzeugenCommand.Execute(null);
        Assert.HasCount(6, vm.Statistik);

        vm.NeuerMitarbeiterName = "Philipp";
        vm.MitarbeiterHinzufuegenCommand.Execute(null);

        Assert.HasCount(7, vm.Statistik);
        Assert.AreEqual(53, vm.Statistik.Sum(s => s.Reinigungen));
    }

    [TestMethod]
    public void Ein_umbenannter_Mitarbeiter_erscheint_unter_dem_neuen_Namen_im_Plan()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);
        vm.PlanErzeugenCommand.Execute(null);

        vm.Mitarbeiter[0].Name = "Ralf Schmidt";

        Assert.IsTrue(vm.Statistik.Any(s => s.Name == "Ralf Schmidt"));
        Assert.IsTrue(vm.Wochen.Any(w => w.Name == "Ralf Schmidt"));
    }

    [TestMethod]
    public void Ein_leerer_Name_wird_nicht_uebernommen()
    {
        MainViewModel vm = ViewModel();

        vm.Mitarbeiter[0].Name = "   ";

        Assert.AreEqual("Ralf", vm.Mitarbeiter[0].Name);
    }

    [TestMethod]
    public void Verschwindet_der_Plan_durch_eine_Teamaenderung_wird_der_Grund_genannt()
    {
        // Von drei auf zwei Personen: damit gibt es keinen Plan mehr, der F1, F2 und F3
        // gleichzeitig erfuellt. Die Liste leert sich sichtbar - der Benutzer muss
        // erfahren, warum, und nicht nur "Mario wurde entfernt" lesen.
        MainViewModel vm = ViewModel(anzahlPersonen: 3);
        vm.PlanErzeugenCommand.Execute(null);
        Assert.IsTrue(vm.HatPlan);

        vm.AusgewaehlterMitarbeiter = vm.Mitarbeiter[2];
        vm.MitarbeiterEntfernenCommand.Execute(null);

        Assert.IsFalse(vm.HatPlan);
        Assert.IsEmpty(vm.Wochen);
        StringAssert.Contains(vm.StatusMeldung, "F1, F2 und F3");
    }

    [TestMethod]
    public void Ein_leeres_Team_sperrt_das_Erzeugen()
    {
        MainViewModel vm = ViewModel();

        while (vm.Mitarbeiter.Count > 0)
        {
            vm.AusgewaehlterMitarbeiter = vm.Mitarbeiter[0];
            vm.MitarbeiterEntfernenCommand.Execute(null);
        }

        Assert.IsFalse(vm.PlanErzeugenCommand.CanExecute(null));
        Assert.IsFalse(vm.SpeichernCommand.CanExecute(null));
    }

    // ----- Speichern und Laden (A9) -----

    [TestMethod]
    public void Speichern_legt_Mitarbeiter_und_Plan_ab()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);
        vm.PlanErzeugenCommand.Execute(null);
        _dialog.AntwortSpeichern = @"C:\test\kaffeeplan.json";

        vm.SpeichernCommand.Execute(null);

        Assert.AreEqual(@"C:\test\kaffeeplan.json", _speicher.ZuletztGespeichertNach);
        StringAssert.Contains(vm.StatusMeldung, "Gespeichert");
    }

    [TestMethod]
    public void Ein_abgebrochener_Speicherdialog_tut_nichts()
    {
        MainViewModel vm = ViewModel();
        _dialog.AntwortSpeichern = null;

        vm.SpeichernCommand.Execute(null);

        Assert.IsNull(_speicher.ZuletztGespeichertNach);
        Assert.AreEqual(string.Empty, vm.StatusMeldung);
    }

    [TestMethod]
    public void Laden_ersetzt_Team_und_Plan()
    {
        List<Mitarbeiter> anderesTeam = Testdaten.Team(4);
        Dienstplan plan = new Dienstplaner().Erzeuge(2027, anderesTeam).Plan!;
        _speicher.Hinterlege(@"C:\test\alt.json", new Speicherstand(anderesTeam, plan));
        _dialog.AntwortOeffnen = @"C:\test\alt.json";

        MainViewModel vm = ViewModel(heutigesJahr: 2026);
        vm.LadenCommand.Execute(null);

        Assert.HasCount(4, vm.Mitarbeiter);
        Assert.AreEqual("2027", vm.JahrEingabe);
        Assert.HasCount(52, vm.Wochen);
        Assert.IsTrue(vm.HatPlan);
    }

    [TestMethod]
    public void Eine_kaputte_Datei_meldet_den_Fehler_statt_abzustuerzen()
    {
        _speicher.FehlerBeimLaden = new SpeicherAusnahme("Die Datei ist keine gueltige Kaffeeplan-Datei.");
        _dialog.AntwortOeffnen = @"C:\test\kaputt.json";

        MainViewModel vm = ViewModel();
        vm.LadenCommand.Execute(null);

        StringAssert.Contains(vm.StatusMeldung, "keine gueltige Kaffeeplan-Datei");
        Assert.IsFalse(vm.HatPlan);
    }

    [TestMethod]
    public void Ein_Speicherfehler_meldet_sich_statt_abzustuerzen()
    {
        MainViewModel vm = ViewModel();
        _speicher.FehlerBeimSpeichern = new SpeicherAusnahme("Kein Zugriff auf die Datei.");
        _dialog.AntwortSpeichern = @"C:\gesperrt\kaffeeplan.json";

        vm.SpeichernCommand.Execute(null);

        StringAssert.Contains(vm.StatusMeldung, "Kein Zugriff");
    }

    // ----- CSV-Export (A10) -----

    [TestMethod]
    public void Ohne_Plan_ist_der_CSV_Export_gesperrt()
    {
        MainViewModel vm = ViewModel();

        Assert.IsFalse(vm.CsvExportierenCommand.CanExecute(null));

        vm.PlanErzeugenCommand.Execute(null);
        Assert.IsTrue(vm.CsvExportierenCommand.CanExecute(null));
    }

    [TestMethod]
    public void Der_CSV_Export_schlaegt_einen_Dateinamen_mit_Jahr_vor()
    {
        MainViewModel vm = ViewModel(heutigesJahr: 2026);
        vm.PlanErzeugenCommand.Execute(null);
        _dialog.AntwortSpeichern = @"C:\test\plan.csv";

        vm.CsvExportierenCommand.Execute(null);

        Assert.AreEqual("kaffeeplan-2026.csv", _dialog.LetzterSpeichernVorschlag);
        Assert.AreEqual(@"C:\test\plan.csv", _csv.ZuletztNach);
        Assert.AreEqual(53, _csv.ZuletztExportierterPlan!.Wochenanzahl);
    }

    [TestMethod]
    public void Ein_fehlgeschlagener_CSV_Export_meldet_sich_statt_abzustuerzen()
    {
        MainViewModel vm = ViewModel();
        vm.PlanErzeugenCommand.Execute(null);
        _dialog.AntwortSpeichern = @"C:\gesperrt\plan.csv";
        _csv.Fehler = new IOException("Die Datei wird von Excel verwendet.");

        vm.CsvExportierenCommand.Execute(null);

        StringAssert.Contains(vm.StatusMeldung, "Excel");
    }

    // ----- Bindung -----

    [TestMethod]
    public void Aenderungen_werden_der_Oberflaeche_gemeldet()
    {
        MainViewModel vm = ViewModel();
        var gemeldet = new List<string?>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => gemeldet.Add(e.PropertyName);

        vm.JahrEingabe = "2030";
        vm.PlanErzeugenCommand.Execute(null);

        CollectionAssert.Contains(gemeldet, nameof(MainViewModel.JahrEingabe));
        CollectionAssert.Contains(gemeldet, nameof(MainViewModel.StatusMeldung));
        CollectionAssert.Contains(gemeldet, nameof(MainViewModel.HatPlan));
    }
}
