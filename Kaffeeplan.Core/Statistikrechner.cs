namespace Kaffeeplan.Core;

/// <summary>Wie oft eine Person im Plan reinigt und wie oft sie zusaetzlich den Filter tauscht.</summary>
public sealed record Mitarbeiterstatistik(Mitarbeiter Mitarbeiter, int Reinigungen, int Filtertausche)
{
    public override string ToString() =>
        $"{Mitarbeiter.Name}: {Reinigungen} Reinigungen, {Filtertausche} Filtertausche";
}

/// <summary>Grundlage fuer die Statistik-Ansicht (A12).</summary>
public static class Statistikrechner
{
    /// <summary>
    /// Zaehlt Reinigungen und Filtertausche je Person, in der Reihenfolge der
    /// Mitarbeiterliste. Personen ohne Einsatz erscheinen mit 0 - sonst faellt
    /// genau der Fall unter den Tisch, den F1 und F2 verhindern sollen.
    /// </summary>
    public static IReadOnlyList<Mitarbeiterstatistik> Berechne(
        Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(mitarbeiter);

        var reinigungen = mitarbeiter.ToDictionary(m => m.Id, _ => 0);
        var filter = mitarbeiter.ToDictionary(m => m.Id, _ => 0);

        foreach (Wocheneintrag eintrag in plan.Eintraege)
        {
            if (!reinigungen.ContainsKey(eintrag.MitarbeiterId))
            {
                continue;
            }

            reinigungen[eintrag.MitarbeiterId]++;
            if (eintrag.IstFilterwoche)
            {
                filter[eintrag.MitarbeiterId]++;
            }
        }

        return mitarbeiter
            .Select(m => new Mitarbeiterstatistik(m, reinigungen[m.Id], filter[m.Id]))
            .ToList();
    }
}
