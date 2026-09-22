using System.Text;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class CsvExporterTests
{
    private static string AlsText(Dienstplan plan, IReadOnlyList<Mitarbeiter> team)
    {
        using var writer = new StringWriter();
        CsvExporter.Schreibe(writer, plan, team);
        return writer.ToString();
    }

    private static string[] Zeilen(string csv) =>
        csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [TestMethod]
    public void Die_Kopfzeile_benennt_die_Spalten()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2025, team).Plan!;

        Assert.AreEqual("KW;Von;Bis;Mitarbeiter;Filtertausch", Zeilen(AlsText(plan, team))[0]);
    }

    [TestMethod]
    [DataRow(2025, 52)]
    [DataRow(2026, 53)]
    public void Auf_die_Kopfzeile_folgt_eine_Zeile_je_Kalenderwoche(int jahr, int wochen)
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(jahr, team).Plan!;

        Assert.HasCount(wochen + 1, Zeilen(AlsText(plan, team)));
    }

    [TestMethod]
    public void Die_erste_Zeile_von_2026_enthaelt_Woche_Datumsbereich_und_Filtertausch()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        string[] felder = Zeilen(AlsText(plan, team))[1].Split(';');

        Assert.AreEqual("1", felder[0]);
        Assert.AreEqual("29.12.2025", felder[1]);
        Assert.AreEqual("04.01.2026", felder[2]);
        Assert.AreEqual("ja", felder[4]);
    }

    [TestMethod]
    public void Eine_Woche_ohne_Filtertausch_ist_als_solche_ausgewiesen()
    {
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(2026, team).Plan!;

        string[] zeilen = Zeilen(AlsText(plan, team));

        Assert.AreEqual("nein", zeilen[2].Split(';')[4]);
        Assert.AreEqual(7, zeilen.Skip(1).Count(z => z.EndsWith(";ja")));
    }

    [TestMethod]
    public void Ein_Semikolon_im_Namen_wird_maskiert()
    {
        var team = new List<Mitarbeiter>
        {
            new(Testdaten.Id(0), "Schmidt; Ralf"),
            new(Testdaten.Id(1), "Jochen"),
            new(Testdaten.Id(2), "Mario"),
        };
        Dienstplan plan = new Dienstplaner().Erzeuge(2025, team).Plan!;

        string csv = AlsText(plan, team);

        StringAssert.Contains(csv, "\"Schmidt; Ralf\"");
    }

    [TestMethod]
    public void Ein_Anfuehrungszeichen_im_Namen_wird_verdoppelt()
    {
        var team = new List<Mitarbeiter>
        {
            new(Testdaten.Id(0), "Ralf \"Chef\" Schmidt"),
            new(Testdaten.Id(1), "Jochen"),
            new(Testdaten.Id(2), "Mario"),
        };
        Dienstplan plan = new Dienstplaner().Erzeuge(2025, team).Plan!;

        StringAssert.Contains(AlsText(plan, team), "\"Ralf \"\"Chef\"\" Schmidt\"");
    }

    [TestMethod]
    public void Die_Datei_beginnt_mit_der_UTF8_BOM_damit_Excel_Umlaute_richtig_liest()
    {
        var team = new List<Mitarbeiter>
        {
            new(Testdaten.Id(0), "Jürgen"),
            new(Testdaten.Id(1), "Jochen"),
            new(Testdaten.Id(2), "Mario"),
        };
        Dienstplan plan = new Dienstplaner().Erzeuge(2025, team).Plan!;

        using var strom = new MemoryStream();
        CsvExporter.Schreibe(strom, plan, team);
        byte[] inhalt = strom.ToArray();

        CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, inhalt.Take(3).ToArray());
        StringAssert.Contains(Encoding.UTF8.GetString(inhalt), "Jürgen");
    }

    [TestMethod]
    public void Auch_der_Plan_des_letzten_gueltigen_Jahres_laesst_sich_exportieren()
    {
        // Der Export schreibt zu jeder Woche deren Sonntag. Am oberen Rand des
        // Jahresbereichs war dieser Sonntag frueher nicht darstellbar, und der Export
        // brach mit einer Ausnahme ab (A10, A11).
        List<Mitarbeiter> team = Testdaten.Team(6);
        Dienstplan plan = new Dienstplaner().Erzeuge(Kalenderwoche.MaxJahr, team).Plan!;

        using var strom = new MemoryStream();
        CsvExporter.Schreibe(strom, plan, team);

        string inhalt = Encoding.UTF8.GetString(strom.ToArray());
        Assert.HasCount(
            plan.Wochenanzahl + 1,
            inhalt.TrimEnd('\r', '\n').Split("\r\n"));
    }
}
