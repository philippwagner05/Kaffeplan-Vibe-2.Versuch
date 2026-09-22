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

    /// <summary>
    /// Das letzte planbare Jahr. Bewusst 9998 und nicht 9999: die letzte ISO-Woche des
    /// Jahres 9999 beginnt am Montag, dem 27.12.9999, und endet am Sonntag, dem
    /// 02.01.10000 - dieser Sonntag liegt hinter <see cref="DateOnly.MaxValue"/> und
    /// laesst sich nicht darstellen. Ein Plan fuer 9999 wuerde deshalb erst beim
    /// Anzeigen oder Exportieren mit einer Ausnahme scheitern (A11). Statt das Datum
    /// abzuschneiden und damit falsch zu machen, ist das Jahr hier gar nicht erst
    /// gueltig.
    /// </summary>
    public const int MaxJahr = 9998;

    public Kalenderwoche(int jahr, int woche)
    {
        PruefeJahr(jahr, nameof(jahr));

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
        PruefeJahr(jahr, nameof(jahr));
        return ISOWeek.GetWeeksInYear(jahr);
    }

    /// <summary>
    /// Die Kalenderwoche, in die ein Datum faellt.
    /// Die letzten Tage des Jahres 9999 gehoeren bereits zum ISO-Wochenjahr 9999 und
    /// damit zu keiner darstellbaren Woche mehr - siehe <see cref="MaxJahr"/>.
    /// </summary>
    public static Kalenderwoche VonDatum(DateOnly datum)
    {
        DateTime zeitpunkt = datum.ToDateTime(TimeOnly.MinValue);
        int jahr = ISOWeek.GetYear(zeitpunkt);
        if (jahr > MaxJahr)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datum), datum,
                $"Das Datum liegt im ISO-Wochenjahr {jahr}; darstellbar sind Jahre bis {MaxJahr}.");
        }

        return new Kalenderwoche(jahr, ISOWeek.GetWeekOfYear(zeitpunkt));
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

    /// <summary>
    /// Die folgende Woche, ueber die Jahresgrenze hinweg.
    /// Nach der letzten Woche des Jahres <see cref="MaxJahr"/> gibt es keine mehr.
    /// </summary>
    public Kalenderwoche Naechste()
    {
        if (Woche < WochenImJahr(Jahr))
        {
            return new Kalenderwoche(Jahr, Woche + 1);
        }

        if (Jahr >= MaxJahr)
        {
            throw new InvalidOperationException(
                $"Nach {this} gibt es keine darstellbare Kalenderwoche mehr.");
        }

        return new Kalenderwoche(Jahr + 1, 1);
    }

    /// <summary>
    /// Die vorangehende Woche, ueber die Jahresgrenze hinweg.
    /// Vor der ersten Woche des Jahres <see cref="MinJahr"/> gibt es keine mehr.
    /// </summary>
    public Kalenderwoche Vorherige()
    {
        if (Woche > 1)
        {
            return new Kalenderwoche(Jahr, Woche - 1);
        }

        if (Jahr <= MinJahr)
        {
            throw new InvalidOperationException(
                $"Vor {this} gibt es keine darstellbare Kalenderwoche.");
        }

        return new Kalenderwoche(Jahr - 1, WochenImJahr(Jahr - 1));
    }

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

    private static void PruefeJahr(int jahr, string parametername)
    {
        if (jahr is < MinJahr or > MaxJahr)
        {
            throw new ArgumentOutOfRangeException(
                parametername, jahr, $"Das Jahr muss zwischen {MinJahr} und {MaxJahr} liegen.");
        }
    }
}
