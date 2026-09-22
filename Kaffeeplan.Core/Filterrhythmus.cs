namespace Kaffeeplan.Core;

/// <summary>
/// Legt fest, in welchen Kalenderwochen zusaetzlich der Filter getauscht wird.
/// Laut Aufgabenstellung beginnt das in KW 1 und wiederholt sich alle 8 Wochen,
/// also KW 1, 9, 17, 25, 33, 41, 49. Der Rhythmus ist konfigurierbar statt
/// fest verdrahtet, damit sich die Regel aendern laesst, ohne den Planer anzufassen.
/// </summary>
public sealed class Filterrhythmus
{
    /// <summary>Der in der Aufgabenstellung vorgegebene Rhythmus: ab KW 1, alle 8 Wochen.</summary>
    public static Filterrhythmus Standard { get; } = new(startwoche: 1, intervall: 8);

    public Filterrhythmus(int startwoche, int intervall)
    {
        if (startwoche < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startwoche), startwoche, "Die Startwoche muss mindestens 1 sein.");
        }

        if (intervall < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intervall), intervall, "Das Intervall muss mindestens 1 sein.");
        }

        Startwoche = startwoche;
        Intervall = intervall;
    }

    public int Startwoche { get; }

    public int Intervall { get; }

    /// <summary>Faellt in dieser Wochennummer zusaetzlich ein Filtertausch an?</summary>
    public bool IstFilterwoche(int woche) =>
        woche >= Startwoche && (woche - Startwoche) % Intervall == 0;

    /// <summary>Alle Filterwochen eines Jahres, aufsteigend. Bei 52 wie bei 53 Wochen sind das 7 Termine.</summary>
    public IReadOnlyList<int> Filterwochen(int jahr)
    {
        int wochenImJahr = Kalenderwoche.WochenImJahr(jahr);
        var wochen = new List<int>();
        for (int woche = Startwoche; woche <= wochenImJahr; woche += Intervall)
        {
            wochen.Add(woche);
        }

        return wochen;
    }
}
