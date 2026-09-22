namespace Kaffeeplan.Core;

/// <summary>Ein konkreter Regelverstoss mit Angabe der verletzten Regel.</summary>
public sealed record Regelverstoss(string Regel, string Beschreibung)
{
    public override string ToString() => $"{Regel}: {Beschreibung}";
}

/// <summary>Das Ergebnis der Pruefung eines Plans gegen F1, F2 und F3.</summary>
public sealed class Pruefergebnis
{
    internal Pruefergebnis(
        IReadOnlyList<Regelverstoss> verstoesse,
        int reinigungsSpanne,
        int filterSpanne)
    {
        Verstoesse = verstoesse;
        ReinigungsSpanne = reinigungsSpanne;
        FilterSpanne = filterSpanne;
    }

    public IReadOnlyList<Regelverstoss> Verstoesse { get; }

    /// <summary>Differenz zwischen der Person mit den meisten und der mit den wenigsten Reinigungen.</summary>
    public int ReinigungsSpanne { get; }

    /// <summary>Differenz zwischen der Person mit den meisten und der mit den wenigsten Filtertauschen.</summary>
    public int FilterSpanne { get; }

    public bool F1Erfuellt => Verstoesse.All(v => v.Regel != Regelpruefer.F1);

    public bool F2Erfuellt => Verstoesse.All(v => v.Regel != Regelpruefer.F2);

    public bool F3Erfuellt => Verstoesse.All(v => v.Regel != Regelpruefer.F3);

    public bool AllesErfuellt => Verstoesse.Count == 0;

    public override string ToString() =>
        AllesErfuellt ? "F1, F2 und F3 erfüllt" : string.Join(" | ", Verstoesse);
}

/// <summary>
/// Prueft einen fertigen Plan gegen die drei Fairness-Kriterien aus der Aufgabenstellung.
/// Bewusst unabhaengig vom <see cref="Dienstplaner"/>: der Pruefer kennt den Algorithmus
/// nicht und kann deshalb auch fremde oder von Hand geaenderte Plaene bewerten.
/// </summary>
public static class Regelpruefer
{
    public const string F1 = "F1";
    public const string F2 = "F2";
    public const string F3 = "F3";

    public static Pruefergebnis Pruefe(Dienstplan plan, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(mitarbeiter);

        if (mitarbeiter.Count == 0)
        {
            throw new ArgumentException("Ohne Mitarbeiter lässt sich kein Plan prüfen.", nameof(mitarbeiter));
        }

        Mitarbeiterliste.PruefeEindeutigeIds(mitarbeiter, nameof(mitarbeiter));

        var reinigungen = mitarbeiter.ToDictionary(m => m.Id, _ => 0);
        var filter = mitarbeiter.ToDictionary(m => m.Id, _ => 0);

        foreach (Wocheneintrag eintrag in plan.Eintraege)
        {
            if (!reinigungen.ContainsKey(eintrag.MitarbeiterId))
            {
                // Ein Plan, der eine unbekannte Person enthaelt, ist kein gueltiger Plan
                // fuer dieses Team - das faellt unter F1, weil die Verteilung dann nicht stimmt.
                continue;
            }

            reinigungen[eintrag.MitarbeiterId]++;
            if (eintrag.IstFilterwoche)
            {
                filter[eintrag.MitarbeiterId]++;
            }
        }

        var verstoesse = new List<Regelverstoss>();

        int reinigungsSpanne = reinigungen.Values.Max() - reinigungen.Values.Min();
        if (reinigungsSpanne > 1)
        {
            verstoesse.Add(new Regelverstoss(
                F1,
                $"Die Reinigungen sind ungleich verteilt: {reinigungen.Values.Min()} bis " +
                $"{reinigungen.Values.Max()}, Differenz {reinigungsSpanne}."));
        }

        int filterSpanne = filter.Values.Max() - filter.Values.Min();
        if (filterSpanne > 1)
        {
            verstoesse.Add(new Regelverstoss(
                F2,
                $"Die Filtertausche sind ungleich verteilt: {filter.Values.Min()} bis " +
                $"{filter.Values.Max()}, Differenz {filterSpanne}."));
        }

        for (int i = 1; i < plan.Eintraege.Count; i++)
        {
            Wocheneintrag vorherige = plan.Eintraege[i - 1];
            Wocheneintrag aktuelle = plan.Eintraege[i];

            if (vorherige.MitarbeiterId == aktuelle.MitarbeiterId)
            {
                string name = mitarbeiter.FirstOrDefault(m => m.Id == aktuelle.MitarbeiterId)?.Name ?? "Unbekannt";
                verstoesse.Add(new Regelverstoss(
                    F3,
                    $"{name} ist in {vorherige.Woche} und {aktuelle.Woche} hintereinander dran."));
            }
        }

        return new Pruefergebnis(verstoesse, reinigungsSpanne, filterSpanne);
    }
}
