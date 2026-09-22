namespace Kaffeeplan.Core;

/// <summary>
/// Was gespeichert und geladen wird: die Mitarbeiterliste und - sofern schon
/// erzeugt - der Plan (A9).
/// </summary>
public sealed class Speicherstand
{
    public Speicherstand(IReadOnlyList<Mitarbeiter> mitarbeiter, Dienstplan? plan)
    {
        ArgumentNullException.ThrowIfNull(mitarbeiter);
        Mitarbeiter = mitarbeiter;
        Plan = plan;
    }

    public static Speicherstand Leer { get; } = new(Array.Empty<Mitarbeiter>(), null);

    public IReadOnlyList<Mitarbeiter> Mitarbeiter { get; }

    public Dienstplan? Plan { get; }
}

/// <summary>Fehler beim Lesen oder Schreiben des Speicherstands.</summary>
public sealed class SpeicherAusnahme : Exception
{
    public SpeicherAusnahme(string meldung) : base(meldung)
    {
    }

    public SpeicherAusnahme(string meldung, Exception ursache) : base(meldung, ursache)
    {
    }
}
