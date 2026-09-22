using Kaffeeplan.App.Infrastructure;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

/// <summary>Eine feste Uhr, damit das vorbelegte Jahr im Test nicht vom Kalender abhaengt.</summary>
internal sealed class FesteUhr(int jahr) : IClock
{
    public DateOnly Heute { get; } = new(jahr, 6, 15);
}

/// <summary>
/// Ein Speicher, der im Arbeitsspeicher bleibt. Damit laufen die ViewModel-Tests
/// ohne eine einzige Datei auf der Platte.
/// </summary>
internal sealed class SpeicherDoppel : IDienstplanSpeicher
{
    private readonly Dictionary<string, Speicherstand> _dateien = [];

    public string? ZuletztGespeichertNach { get; private set; }

    public string? ZuletztGeladenAus { get; private set; }

    public SpeicherAusnahme? FehlerBeimLaden { get; set; }

    public SpeicherAusnahme? FehlerBeimSpeichern { get; set; }

    public void Hinterlege(string pfad, Speicherstand stand) => _dateien[pfad] = stand;

    public void Speichere(Stream ziel, Speicherstand stand) => throw new NotSupportedException();

    public Speicherstand Lade(Stream quelle) => throw new NotSupportedException();

    public void SpeichereDatei(string pfad, Speicherstand stand)
    {
        if (FehlerBeimSpeichern is not null)
        {
            throw FehlerBeimSpeichern;
        }

        _dateien[pfad] = stand;
        ZuletztGespeichertNach = pfad;
    }

    public Speicherstand LadeDatei(string pfad)
    {
        if (FehlerBeimLaden is not null)
        {
            throw FehlerBeimLaden;
        }

        ZuletztGeladenAus = pfad;
        return _dateien.TryGetValue(pfad, out Speicherstand? stand) ? stand : Speicherstand.Leer;
    }
}

/// <summary>Ein Dateidialog, der vorher festgelegte Antworten gibt - oder Abbruch (null).</summary>
internal sealed class DialogDoppel : IDateiDialog
{
    public string? AntwortOeffnen { get; set; }

    public string? AntwortSpeichern { get; set; }

    public string? LetzterSpeichernVorschlag { get; private set; }

    public string? DateiZumOeffnen(string titel, string filter) => AntwortOeffnen;

    public string? DateiZumSpeichern(string titel, string filter, string dateivorschlag)
    {
        LetzterSpeichernVorschlag = dateivorschlag;
        return AntwortSpeichern;
    }
}

/// <summary>Ein CSV-Export, der sich nur merkt, was er geschrieben haette.</summary>
internal sealed class CsvExportDoppel : ICsvExport
{
    public string? ZuletztNach { get; private set; }

    public Dienstplan? ZuletztExportierterPlan { get; private set; }

    public Exception? Fehler { get; set; }

    public void Exportiere(string pfad, Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        if (Fehler is not null)
        {
            throw Fehler;
        }

        ZuletztNach = pfad;
        ZuletztExportierterPlan = plan;
    }
}
