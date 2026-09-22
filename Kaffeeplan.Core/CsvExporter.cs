using System.Globalization;
using System.Text;

namespace Kaffeeplan.Core;

/// <summary>
/// Export des Plans als CSV (A10).
/// Trennzeichen ist das Semikolon und die Datei bekommt eine UTF-8-BOM, weil
/// deutsches Excel eine kommagetrennte Datei in eine einzige Spalte legt und
/// ohne BOM die Umlaute zerlegt.
/// </summary>
public static class CsvExporter
{
    public const char Trennzeichen = ';';

    private static readonly string[] Kopfzeile =
        ["KW", "Von", "Bis", "Mitarbeiter", "Filtertausch"];

    /// <summary>Schreibt den Plan in einen TextWriter - so laesst sich der Inhalt ohne Datei pruefen.</summary>
    public static void Schreibe(TextWriter writer, Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(mitarbeiter);

        var namen = mitarbeiter.ToDictionary(m => m.Id, m => m.Name);

        writer.Write(string.Join(Trennzeichen, Kopfzeile.Select(Maskiere)));
        writer.Write("\r\n");

        foreach (Wocheneintrag eintrag in plan.Eintraege)
        {
            string[] felder =
            [
                eintrag.Woche.Woche.ToString(CultureInfo.InvariantCulture),
                eintrag.Woche.Montag.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                eintrag.Woche.Sonntag.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                namen.TryGetValue(eintrag.MitarbeiterId, out string? name) ? name : "Unbekannt",
                eintrag.IstFilterwoche ? "ja" : "nein",
            ];

            writer.Write(string.Join(Trennzeichen, felder.Select(Maskiere)));
            writer.Write("\r\n");
        }
    }

    /// <summary>Schreibt den Plan als UTF-8 mit BOM in einen Stream.</summary>
    public static void Schreibe(Stream ziel, Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        ArgumentNullException.ThrowIfNull(ziel);

        using var writer = new StreamWriter(ziel, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        Schreibe(writer, plan, mitarbeiter);
        writer.Flush();
    }

    public static void SchreibeDatei(string pfad, Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pfad);

        using FileStream datei = File.Create(pfad);
        Schreibe(datei, plan, mitarbeiter);
    }

    /// <summary>
    /// Ein Feld, das Trennzeichen, Anfuehrungszeichen oder Zeilenumbruch enthaelt,
    /// kommt in Anfuehrungszeichen; enthaltene Anfuehrungszeichen werden verdoppelt.
    /// </summary>
    private static string Maskiere(string feld)
    {
        bool mussMaskiert = feld.Contains(Trennzeichen) ||
                            feld.Contains('"') ||
                            feld.Contains('\n') ||
                            feld.Contains('\r');

        return mussMaskiert ? $"\"{feld.Replace("\"", "\"\"")}\"" : feld;
    }
}
