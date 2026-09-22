namespace Kaffeeplan.Core;

/// <summary>
/// Regel fuer jede Mitarbeiterliste: keine Person darf zweimal darin stehen.
/// Alles, was Mitarbeiter ueber ihre Id nachschlaegt, setzt das voraus - ohne die
/// Pruefung scheitert der erste Nachschlage-Aufbau mit einer Meldung ueber doppelte
/// Schluessel, die dem Benutzer nichts sagt.
/// </summary>
public static class Mitarbeiterliste
{
    public const string DublettenMeldung = "Die Mitarbeiterliste enthält dieselbe Person mehrfach.";

    /// <summary>Prueft ohne Ausnahme - fuer Aufrufer, die selbst eine Meldung anzeigen wollen.</summary>
    public static bool HatEindeutigeIds(IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        ArgumentNullException.ThrowIfNull(mitarbeiter);
        return mitarbeiter.Select(m => m.Id).Distinct().Count() == mitarbeiter.Count;
    }

    /// <summary>Vorbedingung fuer Auswertungen, die ohne eindeutige Ids nicht arbeiten koennen.</summary>
    public static void PruefeEindeutigeIds(IReadOnlyList<Mitarbeiter> mitarbeiter, string parameterName)
    {
        if (!HatEindeutigeIds(mitarbeiter))
        {
            throw new ArgumentException(DublettenMeldung, parameterName);
        }
    }
}
