using System.Globalization;

namespace Kaffeeplan.Core;

/// <summary>
/// Eine Kalenderwoche nach ISO 8601.
/// Diese Struktur kapselt <see cref="ISOWeek"/> vollstaendig: im uebrigen Code wird
/// ISOWeek nicht direkt aufgerufen, damit die Wochenlogik an genau einer Stelle liegt.
/// Hinweis: <c>default(Kalenderwoche)</c> ist keine gueltige Woche.
/// </summary>
public readonly record struct Kalenderwoche : IComparable<Kalenderwoche>
{
    public const int MinJahr = 1;
    public const int MaxJahr = 9999;

    public Kalenderwoche(int jahr, int woche)
    {
        if (jahr is < MinJahr or > MaxJahr)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jahr), jahr, $"Das Jahr muss zwischen {MinJahr} und {MaxJahr} liegen.");
        }

        int wochenImJahr = WochenImJahr(jahr);
        if (woche < 1 || woche > wochenImJahr)
        {
            throw new ArgumentOutOfRangeException(
                nameof(woche), woche, $"Das Jahr {jahr} hat {wochenImJahr} Kalenderwochen.");
        }

        Jahr = jahr;
        Woche = woche;
    }

    /// <summary>Das ISO-Wochenjahr. Kann vom Kalenderjahr des Datums abweichen.</summary>
    public int Jahr { get; }

    /// <summary>Die Wochennummer, 1 bis 52 oder 53.</summary>
    public int Woche { get; }

    /// <summary>Anzahl der Kalenderwochen eines Jahres. 2026 und 2032 haben zum Beispiel 53.</summary>
    public static int WochenImJahr(int jahr)
    {
        if (jahr is < MinJahr or > MaxJahr)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jahr), jahr, $"Das Jahr muss zwischen {MinJahr} und {MaxJahr} liegen.");
        }

        return ISOWeek.GetWeeksInYear(jahr);
    }

    /// <summary>Die Kalenderwoche, in die ein Datum faellt.</summary>
    public static Kalenderwoche VonDatum(DateOnly datum)
    {
        DateTime zeitpunkt = datum.ToDateTime(TimeOnly.MinValue);
        return new Kalenderwoche(ISOWeek.GetYear(zeitpunkt), ISOWeek.GetWeekOfYear(zeitpunkt));
    }

    /// <summary>Alle Kalenderwochen eines Jahres, aufsteigend.</summary>
    public static IReadOnlyList<Kalenderwoche> Jahreswochen(int jahr)
    {
        int anzahl = WochenImJahr(jahr);
        var wochen = new List<Kalenderwoche>(anzahl);
        for (int woche = 1; woche <= anzahl; woche++)
        {
            wochen.Add(new Kalenderwoche(jahr, woche));
        }

        return wochen;
    }

    public DateOnly Montag => DateOnly.FromDateTime(ISOWeek.ToDateTime(Jahr, Woche, DayOfWeek.Monday));

    public DateOnly Sonntag => Montag.AddDays(6);

    /// <summary>Die folgende Woche, ueber die Jahresgrenze hinweg.</summary>
    public Kalenderwoche Naechste() =>
        Woche < WochenImJahr(Jahr) ? new Kalenderwoche(Jahr, Woche + 1) : new Kalenderwoche(Jahr + 1, 1);

    /// <summary>Die vorangehende Woche, ueber die Jahresgrenze hinweg.</summary>
    public Kalenderwoche Vorherige() =>
        Woche > 1 ? new Kalenderwoche(Jahr, Woche - 1) : new Kalenderwoche(Jahr - 1, WochenImJahr(Jahr - 1));

    public int CompareTo(Kalenderwoche andere)
    {
        int nachJahr = Jahr.CompareTo(andere.Jahr);
        return nachJahr != 0 ? nachJahr : Woche.CompareTo(andere.Woche);
    }

    /// <summary>Textform "2026-W39" - stabil und damit als Dateiformat geeignet.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Jahr:D4}-W{Woche:D2}");

    public static Kalenderwoche Parse(string text) =>
        TryParse(text, out Kalenderwoche woche)
            ? woche
            : throw new FormatException($"'{text}' ist keine Kalenderwoche im Format 2026-W39.");

    public static bool TryParse(string? text, out Kalenderwoche ergebnis)
    {
        ergebnis = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] teile = text.Trim().Split('W', StringSplitOptions.None);
        if (teile.Length != 2 || !teile[0].EndsWith('-'))
        {
            return false;
        }

        string jahrText = teile[0][..^1];
        if (!int.TryParse(jahrText, NumberStyles.None, CultureInfo.InvariantCulture, out int jahr) ||
            !int.TryParse(teile[1], NumberStyles.None, CultureInfo.InvariantCulture, out int woche))
        {
            return false;
        }

        if (jahr is < MinJahr or > MaxJahr || woche < 1 || woche > ISOWeek.GetWeeksInYear(jahr))
        {
            return false;
        }

        ergebnis = new Kalenderwoche(jahr, woche);
        return true;
    }
}
