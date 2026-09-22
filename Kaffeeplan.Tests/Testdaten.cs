using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

/// <summary>
/// Gemeinsame Testdaten. Die Ids sind fest vergeben statt zufaellig, damit ein
/// fehlgeschlagener Test immer dieselbe Ausgabe liefert.
/// </summary>
internal static class Testdaten
{
    private static readonly string[] TeamAusDerAufgabenstellung =
        ["Ralf", "Jochen", "Mario", "Gabriel", "Ehsan", "Shariyar"];

    public static Guid Id(int index) => new($"00000000-0000-0000-0000-{index + 1:D12}");

    /// <summary>Ein Team der gewuenschten Groesse, die ersten sechs mit den Namen aus der Aufgabenstellung.</summary>
    public static List<Mitarbeiter> Team(int anzahl)
    {
        var liste = new List<Mitarbeiter>(anzahl);
        for (int i = 0; i < anzahl; i++)
        {
            string name = i < TeamAusDerAufgabenstellung.Length
                ? TeamAusDerAufgabenstellung[i]
                : $"Person {i + 1}";

            liste.Add(new Mitarbeiter(Id(i), name));
        }

        return liste;
    }

    /// <summary>
    /// Baut einen vollstaendigen Plan aus einer Zuordnungsvorschrift Woche -> Personenindex.
    /// Damit lassen sich absichtlich fehlerhafte Plaene erzeugen, um den Regelpruefer
    /// selbst zu pruefen.
    /// </summary>
    public static Dienstplan PlanAus(int jahr, IReadOnlyList<Mitarbeiter> team, Func<int, int> personFuerWoche)
    {
        Filterrhythmus rhythmus = Filterrhythmus.Standard;
        int wochen = Kalenderwoche.WochenImJahr(jahr);
        var eintraege = new List<Wocheneintrag>(wochen);

        for (int woche = 1; woche <= wochen; woche++)
        {
            eintraege.Add(new Wocheneintrag(
                new Kalenderwoche(jahr, woche),
                team[personFuerWoche(woche)].Id,
                rhythmus.IstFilterwoche(woche)));
        }

        return new Dienstplan(jahr, eintraege);
    }

    /// <summary>Die naive Reihum-Zuordnung - genau die, die F2 verletzt.</summary>
    public static Func<int, int> NaiveRotation(int anzahlPersonen) =>
        woche => (woche - 1) % anzahlPersonen;
}
