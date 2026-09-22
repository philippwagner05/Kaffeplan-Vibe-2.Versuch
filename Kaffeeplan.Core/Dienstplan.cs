namespace Kaffeeplan.Core;

/// <summary>
/// Ein vollstaendiger Dienstplan fuer ein Kalenderjahr.
/// Die Invariante "jede Kalenderwoche hat genau eine zustaendige Person" (A2) wird
/// hier erzwungen: ein Plan mit Luecke oder Dublette laesst sich gar nicht erst bauen.
/// </summary>
public sealed class Dienstplan
{
    public Dienstplan(int jahr, IEnumerable<Wocheneintrag> eintraege)
    {
        ArgumentNullException.ThrowIfNull(eintraege);

        int erwartet = Kalenderwoche.WochenImJahr(jahr);
        var sortiert = eintraege.OrderBy(e => e.Woche.Woche).ToList();

        if (sortiert.Any(e => e is null))
        {
            throw new ArgumentException("Der Plan darf keine leeren Eintraege enthalten.", nameof(eintraege));
        }

        if (sortiert.Count != erwartet)
        {
            throw new ArgumentException(
                $"Das Jahr {jahr} hat {erwartet} Kalenderwochen, der Plan enthaelt {sortiert.Count} Eintraege.",
                nameof(eintraege));
        }

        for (int i = 0; i < sortiert.Count; i++)
        {
            Wocheneintrag eintrag = sortiert[i];

            if (eintrag.Woche.Jahr != jahr)
            {
                throw new ArgumentException(
                    $"Der Eintrag {eintrag.Woche} gehoert nicht zum Jahr {jahr}.", nameof(eintraege));
            }

            if (eintrag.Woche.Woche != i + 1)
            {
                throw new ArgumentException(
                    $"Die Kalenderwoche {i + 1} fehlt oder ist doppelt belegt.", nameof(eintraege));
            }
        }

        Jahr = jahr;
        Eintraege = sortiert;
    }

    public int Jahr { get; }

    /// <summary>Alle Wochen des Jahres, aufsteigend nach Wochennummer.</summary>
    public IReadOnlyList<Wocheneintrag> Eintraege { get; }

    public int Wochenanzahl => Eintraege.Count;

    /// <summary>Die Wochen, in denen zusaetzlich der Filter getauscht wird.</summary>
    public IEnumerable<Wocheneintrag> Filterwochen => Eintraege.Where(e => e.IstFilterwoche);

    /// <summary>Der Eintrag zu einer Wochennummer.</summary>
    public Wocheneintrag this[int wochennummer]
    {
        get
        {
            if (wochennummer < 1 || wochennummer > Eintraege.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wochennummer), wochennummer, $"Das Jahr {Jahr} hat {Eintraege.Count} Kalenderwochen.");
            }

            return Eintraege[wochennummer - 1];
        }
    }

    public Wocheneintrag Eintrag(Kalenderwoche woche)
    {
        if (woche.Jahr != Jahr)
        {
            throw new ArgumentException($"Die Woche {woche} gehoert nicht zum Jahr {Jahr}.", nameof(woche));
        }

        return this[woche.Woche];
    }
}
