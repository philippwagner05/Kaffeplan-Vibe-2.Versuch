using System.Text;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

/// <summary>
/// Prueft die dateibasierten Wege durchgehend - die Stream-Varianten sind an anderer
/// Stelle abgedeckt, aber SpeichereDatei, LadeDatei und SchreibeDatei kapseln
/// Dateizugriff und Zeichensatz und muessen selbst belegt sein (A9, A10).
/// Geschrieben wird ins Ausgabeverzeichnis des Testlaufs, das nicht im Repository liegt.
/// </summary>
[TestClass]
public sealed class DateiRundlaufTests
{
    private string _pfad = null!;

    [TestInitialize]
    public void Vorbereiten() =>
        _pfad = Path.Combine(AppContext.BaseDirectory, $"rundlauf-{Guid.NewGuid():N}");

    [TestCleanup]
    public void Aufraeumen()
    {
        foreach (string datei in new[] { _pfad + ".json", _pfad + ".csv" })
        {
            if (File.Exists(datei))
            {
                File.Delete(datei);
            }
        }
    }

    [TestMethod]
    public void Ein_Speicherstand_uebersteht_den_Weg_ueber_eine_echte_Datei()
    {
        var speicher = new JsonDienstplanSpeicher();
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;
        string datei = _pfad + ".json";

        speicher.SpeichereDatei(datei, new Speicherstand(team, plan));
        Speicherstand geladen = speicher.LadeDatei(datei);

        Assert.IsTrue(File.Exists(datei));
        CollectionAssert.AreEqual(team, geladen.Mitarbeiter.ToList());
        Assert.AreEqual(2026, geladen.Plan!.Jahr);
        Assert.AreEqual(53, geladen.Plan.Wochenanzahl);
        CollectionAssert.AreEqual(
            plan.Eintraege.Select(e => e.MitarbeiterId).ToArray(),
            geladen.Plan.Eintraege.Select(e => e.MitarbeiterId).ToArray());
        Assert.IsTrue(Regelpruefer.Pruefe(geladen.Plan, geladen.Mitarbeiter).AllesErfuellt);
    }

    [TestMethod]
    public void Ein_zweites_Speichern_ersetzt_die_Datei_vollstaendig()
    {
        var speicher = new JsonDienstplanSpeicher();
        string datei = _pfad + ".json";

        speicher.SpeichereDatei(datei, new Speicherstand(Testdaten.Team(8), null));
        speicher.SpeichereDatei(datei, new Speicherstand(Testdaten.Team(3), null));

        Assert.HasCount(3, speicher.LadeDatei(datei).Mitarbeiter);
    }

    [TestMethod]
    public void Die_CSV_Datei_ist_UTF8_mit_BOM_und_semikolongetrennt()
    {
        var team = new List<Mitarbeiter>
        {
            new(Testdaten.Id(0), "Jürgen Groß"),
            new(Testdaten.Id(1), "Jochen"),
            new(Testdaten.Id(2), "Mario"),
        };
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;
        string datei = _pfad + ".csv";

        CsvExporter.SchreibeDatei(datei, plan, team);

        byte[] rohbytes = File.ReadAllBytes(datei);
        CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, rohbytes.Take(3).ToArray());

        string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8);
        Assert.HasCount(54, zeilen, "Kopfzeile plus 53 Kalenderwochen");
        Assert.AreEqual("KW;Von;Bis;Mitarbeiter;Filtertausch", zeilen[0]);
        Assert.AreEqual("1;29.12.2025;04.01.2026;Jürgen Groß;ja", zeilen[1]);
        Assert.AreEqual(7, zeilen.Skip(1).Count(z => z.EndsWith(";ja")));
        Assert.IsTrue(zeilen.Skip(1).All(z => z.Count(c => c == ';') == 4));
    }
}
