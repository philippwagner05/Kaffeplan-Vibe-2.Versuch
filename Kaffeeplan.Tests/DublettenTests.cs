using System.Text;
using Kaffeeplan.App.ViewModels;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

/// <summary>
/// Eine von Hand editierte Datei kann dieselbe Mitarbeiter-Id zweimal enthalten.
/// Das Programm darf daran nicht zerbrechen (A11).
/// </summary>
[TestClass]
public sealed class DublettenTests
{
    private static List<Mitarbeiter> TeamMitDublette()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        team[3] = new Mitarbeiter(Testdaten.Id(0), "Ralf (Dublette)");
        return team;
    }

    [TestMethod]
    public void Eine_Datei_mit_doppelter_Mitarbeiter_Id_wird_beim_Laden_abgelehnt()
    {
        string json = """
        {
          "Version": 1,
          "Mitarbeiter": [
            { "Id": "00000000-0000-0000-0000-000000000001", "Name": "Ralf" },
            { "Id": "00000000-0000-0000-0000-000000000001", "Name": "Jochen" }
          ]
        }
        """;

        using var strom = new MemoryStream(Encoding.UTF8.GetBytes(json));

        Assert.ThrowsExactly<SpeicherAusnahme>(() => new JsonDienstplanSpeicher().Lade(strom));
    }

    [TestMethod]
    public void Ein_Plan_der_auf_unbekannte_Mitarbeiter_verweist_wird_abgelehnt()
    {
        // Ein vollstaendiger Plan, dessen Eintraege auf ein anderes Team verweisen als
        // die Mitarbeiterliste der Datei. Ohne Pruefung zeigt die Oberflaeche
        // "Unbekannt" und der Regelpruefer meldet trotzdem "erfuellt", weil er solche
        // Eintraege ueberspringt.
        var speicher = new JsonDienstplanSpeicher();
        Dienstplan plan = new Dienstplaner().Erzeuge(2025, Testdaten.Team(6)).Plan!;
        var fremdeListe = new List<Mitarbeiter> { new(Testdaten.Id(99), "Jemand anderes") };

        using var strom = new MemoryStream();
        speicher.Speichere(strom, new Speicherstand(fremdeListe, plan));
        strom.Position = 0;

        Assert.ThrowsExactly<SpeicherAusnahme>(() => speicher.Lade(strom));
    }

    [TestMethod]
    public void Der_Regelpruefer_lehnt_eine_Liste_mit_Dubletten_verstaendlich_ab()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        ArgumentException fehler = Assert.ThrowsExactly<ArgumentException>(
            () => Regelpruefer.Pruefe(plan, TeamMitDublette()));

        StringAssert.Contains(fehler.Message, "mehrfach");
    }

    [TestMethod]
    public void Der_Statistikrechner_lehnt_eine_Liste_mit_Dubletten_verstaendlich_ab()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        ArgumentException fehler = Assert.ThrowsExactly<ArgumentException>(
            () => Statistikrechner.Berechne(plan, TeamMitDublette()));

        StringAssert.Contains(fehler.Message, "mehrfach");
    }

    [TestMethod]
    public void Der_CSV_Export_lehnt_eine_Liste_mit_Dubletten_verstaendlich_ab()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        using var writer = new StringWriter();

        ArgumentException fehler = Assert.ThrowsExactly<ArgumentException>(
            () => CsvExporter.Schreibe(writer, plan, TeamMitDublette()));

        StringAssert.Contains(fehler.Message, "mehrfach");
    }

    [TestMethod]
    public void Ein_Speicherstand_mit_Dubletten_stuerzt_die_Oberflaeche_nicht_ab()
    {
        var speicher = new SpeicherDoppel();
        var dialog = new DialogDoppel { AntwortOeffnen = @"C:\test\dublette.json" };

        List<Mitarbeiter> team = TeamMitDublette();
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, Testdaten.Team(6)).Plan!;
        speicher.Hinterlege(@"C:\test\dublette.json", new Speicherstand(team, plan));

        var vm = new MainViewModel(
            new Dienstplaner(), speicher, dialog, new CsvExportDoppel(),
            new FesteUhr(2026), Testdaten.Team(6));

        vm.LadenCommand.Execute(null);

        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.StatusMeldung));
        Assert.IsFalse(vm.HatPlan);
    }
}
