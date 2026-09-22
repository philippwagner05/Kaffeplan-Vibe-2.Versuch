using System.Text;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class JsonDienstplanSpeicherTests
{
    private static readonly JsonDienstplanSpeicher Speicher = new();

    private static Speicherstand LadeAus(string json)
    {
        using var strom = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return Speicher.Lade(strom);
    }

    private static string AlsText(Speicherstand stand)
    {
        using var strom = new MemoryStream();
        Speicher.Speichere(strom, stand);
        return Encoding.UTF8.GetString(strom.ToArray());
    }

    [TestMethod]
    public void Mitarbeiter_und_Plan_ueberstehen_das_Speichern_und_Laden()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;
        var original = new Speicherstand(team, plan);

        Speicherstand geladen = LadeAus(AlsText(original));

        CollectionAssert.AreEqual(team, geladen.Mitarbeiter.ToList());
        Assert.IsNotNull(geladen.Plan);
        Assert.AreEqual(2026, geladen.Plan.Jahr);
        Assert.AreEqual(53, geladen.Plan.Wochenanzahl);
        CollectionAssert.AreEqual(
            plan.Eintraege.Select(e => e.MitarbeiterId).ToArray(),
            geladen.Plan.Eintraege.Select(e => e.MitarbeiterId).ToArray());
        CollectionAssert.AreEqual(
            plan.Eintraege.Select(e => e.IstFilterwoche).ToArray(),
            geladen.Plan.Eintraege.Select(e => e.IstFilterwoche).ToArray());
    }

    [TestMethod]
    public void Eine_Mitarbeiterliste_ohne_Plan_laesst_sich_speichern()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);

        Speicherstand geladen = LadeAus(AlsText(new Speicherstand(team, null)));

        Assert.HasCount(6, geladen.Mitarbeiter);
        Assert.IsNull(geladen.Plan);
    }

    [TestMethod]
    public void Die_Kalenderwoche_steht_lesbar_in_der_Datei()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        string json = AlsText(new Speicherstand(team, plan));

        StringAssert.Contains(json, "2026-W01");
        StringAssert.Contains(json, "2026-W53");
        StringAssert.Contains(json, "\"Version\": 1");
    }

    [TestMethod]
    public void Kaputtes_JSON_wird_als_Speicherfehler_gemeldet()
    {
        Assert.ThrowsExactly<SpeicherAusnahme>(() => LadeAus("{ das ist kein JSON"));
    }

    [TestMethod]
    public void Eine_unbekannte_Version_wird_abgelehnt()
    {
        SpeicherAusnahme fehler = Assert.ThrowsExactly<SpeicherAusnahme>(
            () => LadeAus("""{ "Version": 99, "Mitarbeiter": [] }"""));

        StringAssert.Contains(fehler.Message, "99");
    }

    [TestMethod]
    public void Eine_unschluessige_Woche_wird_abgelehnt()
    {
        string json = """
        {
          "Version": 1,
          "Mitarbeiter": [ { "Id": "00000000-0000-0000-0000-000000000001", "Name": "Ralf" } ],
          "Plan": {
            "Jahr": 2025,
            "Eintraege": [ { "Woche": "2025-W53", "MitarbeiterId": "00000000-0000-0000-0000-000000000001", "Filtertausch": false } ]
          }
        }
        """;

        Assert.ThrowsExactly<SpeicherAusnahme>(() => LadeAus(json));
    }

    [TestMethod]
    public void Eine_leere_Datei_wird_abgelehnt()
    {
        Assert.ThrowsExactly<SpeicherAusnahme>(() => LadeAus("null"));
    }

    [TestMethod]
    public void Eine_fehlende_Datei_ist_kein_Fehler_sondern_ein_leerer_Stand()
    {
        string pfad = Path.Combine(AppContext.BaseDirectory, "diese-datei-gibt-es-nicht.json");
        Assert.IsFalse(File.Exists(pfad));

        Speicherstand stand = Speicher.LadeDatei(pfad);

        Assert.IsEmpty(stand.Mitarbeiter);
        Assert.IsNull(stand.Plan);
    }
}
