using Kaffeeplan.Core;

namespace Kaffeeplan.App.Infrastructure;

/// <summary>
/// Auswahl von Dateipfaden. Als Schnittstelle, damit das ViewModel ohne
/// Oberflaeche testbar bleibt - im Test antwortet ein Doppel, in der Anwendung
/// der Windows-Dialog. <c>null</c> bedeutet: der Benutzer hat abgebrochen.
/// </summary>
public interface IDateiDialog
{
    string? DateiZumOeffnen(string titel, string filter);

    string? DateiZumSpeichern(string titel, string filter, string dateivorschlag);
}

/// <summary>Schreibt den CSV-Export. Eigene Schnittstelle, damit im Test keine Datei entsteht.</summary>
public interface ICsvExport
{
    void Exportiere(string pfad, Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter);
}

/// <summary>Der echte Export in eine Datei: Semikolon als Trennzeichen, UTF-8 mit BOM.</summary>
public sealed class CsvDateiExport : ICsvExport
{
    public void Exportiere(string pfad, Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter) =>
        CsvExporter.SchreibeDatei(pfad, plan, mitarbeiter);
}
