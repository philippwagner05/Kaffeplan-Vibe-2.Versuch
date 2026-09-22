namespace Kaffeeplan.Core;

/// <summary>
/// Erzeugt den Jahresdienstplan.
/// </summary>
/// <remarks>
/// <para>
/// Warum nicht einfach reihum? Weil stures Reihum F2 verletzt. Bei 6 Personen und
/// <c>Person = (KW-1) mod 6</c> landen die Filterwochen 1, 9, 17, 25, 33, 41, 49 auf
/// den Indizes 0, 2, 4, 0, 2, 4, 0: drei Personen bekommen 3, 2 und 2 Filtertausche,
/// die anderen drei keinen einzigen. Bei 8 Personen ist es noch schlimmer - alle
/// sieben Filterwochen fallen auf dieselbe Person.
/// </para>
/// <para>
/// Deshalb werden die Filterwochen <b>zuerst</b> verteilt, und zwar reihum auf
/// verschiedene Personen. Damit gilt F2 per Konstruktion. Anschliessend werden die
/// uebrigen Wochen aus festen Kontingenten gefuellt, womit F1 per Konstruktion gilt,
/// und dabei die Person der Vorwoche ausgeschlossen, womit F3 gilt. Faehrt sich die
/// gierige Auswahl fest, wird zurueckgesetzt und ein anderer Zweig probiert.
/// </para>
/// <para>
/// Findet die Suche keine Loesung, ist das ein Ergebnis und kein Fehler: bei einer
/// Person ist F3 unmoeglich, bei zwei Personen erzwingt F3 strikte Abwechslung,
/// wodurch alle (stets ungeraden) Filterwochen dieselbe Person treffen und F2
/// unmoeglich wird.
/// </para>
/// </remarks>
public sealed class Dienstplaner
{
    /// <summary>Obergrenze fuer die Suche, damit ein unloesbarer Fall nicht endlos rechnet.</summary>
    public const int MaxSchritte = 1_000_000;

    private readonly Filterrhythmus _rhythmus;

    public Dienstplaner() : this(Filterrhythmus.Standard)
    {
    }

    public Dienstplaner(Filterrhythmus rhythmus)
    {
        ArgumentNullException.ThrowIfNull(rhythmus);
        _rhythmus = rhythmus;
    }

    public Filterrhythmus Rhythmus => _rhythmus;

    public PlanungsErgebnis Erzeuge(int jahr, IReadOnlyList<Mitarbeiter> mitarbeiter)
    {
        if (mitarbeiter is null || mitarbeiter.Count == 0)
        {
            return PlanungsErgebnis.Fehler(
                "Ohne Mitarbeiter lässt sich kein Dienstplan erzeugen. Bitte mindestens drei Personen anlegen.");
        }

        if (mitarbeiter.Any(m => m is null))
        {
            return PlanungsErgebnis.Fehler("Die Mitarbeiterliste enthält einen leeren Eintrag.");
        }

        if (mitarbeiter.Select(m => m.Id).Distinct().Count() != mitarbeiter.Count)
        {
            return PlanungsErgebnis.Fehler("Die Mitarbeiterliste enthält dieselbe Person mehrfach.");
        }

        if (jahr is < Kalenderwoche.MinJahr or > Kalenderwoche.MaxJahr)
        {
            return PlanungsErgebnis.Fehler(
                $"Das Jahr {jahr} liegt außerhalb des gültigen Bereichs " +
                $"({Kalenderwoche.MinJahr} bis {Kalenderwoche.MaxJahr}).");
        }

        int anzahlPersonen = mitarbeiter.Count;
        int wochen = Kalenderwoche.WochenImJahr(jahr);

        // Schritt 1: Filterwochen reihum auf verschiedene Personen -> F2 per Konstruktion.
        IReadOnlyList<int> filterwochen = _rhythmus.Filterwochen(jahr);
        var festeZuordnung = new int[wochen + 2];
        Array.Fill(festeZuordnung, Suche.Frei);
        for (int k = 0; k < filterwochen.Count; k++)
        {
            festeZuordnung[filterwochen[k]] = k % anzahlPersonen;
        }

        // Schritt 2: Kontingente so verteilen, dass sich die Reinigungen um hoechstens
        // eine unterscheiden -> F1 per Konstruktion.
        int basis = wochen / anzahlPersonen;
        int rest = wochen % anzahlPersonen;
        var kontingent = new int[anzahlPersonen];
        for (int person = 0; person < anzahlPersonen; person++)
        {
            kontingent[person] = basis + (person < rest ? 1 : 0);
        }

        // Schritt 3: Wochen fuellen, Vorwoche und naechste feste Woche ausgeschlossen -> F3.
        var suche = new Suche(wochen, anzahlPersonen, festeZuordnung, kontingent);
        if (!suche.Loese(1))
        {
            return PlanungsErgebnis.Fehler(suche.Abgebrochen
                ? $"Die Suche wurde nach {MaxSchritte} Schritten abgebrochen, ohne einen Plan zu finden."
                : $"Für {anzahlPersonen} " + (anzahlPersonen == 1 ? "Person" : "Personen") +
                  $" gibt es im Jahr {jahr} keinen Plan, der F1, F2 und F3 gleichzeitig erfüllt.");
        }

        var eintraege = new List<Wocheneintrag>(wochen);
        for (int woche = 1; woche <= wochen; woche++)
        {
            eintraege.Add(new Wocheneintrag(
                new Kalenderwoche(jahr, woche),
                mitarbeiter[suche.Belegung[woche]].Id,
                _rhythmus.IstFilterwoche(woche)));
        }

        return PlanungsErgebnis.Erfolg(new Dienstplan(jahr, eintraege));
    }

    /// <summary>
    /// Tiefensuche ueber die Wochen. Die Kandidaten werden nach verbleibendem
    /// Kontingent absteigend probiert - das ist die gierige Reihenfolge, die in der
    /// Praxis sofort durchlaeuft. Bei Gleichstand entscheidet der kleinere Index,
    /// damit derselbe Input immer denselben Plan ergibt.
    /// </summary>
    private sealed class Suche
    {
        public const int Frei = -1;

        private readonly int _wochen;
        private readonly int _anzahlPersonen;
        private readonly int[] _festeZuordnung;
        private readonly int[] _kontingent;
        private int _schritte;

        public Suche(int wochen, int anzahlPersonen, int[] festeZuordnung, int[] kontingent)
        {
            _wochen = wochen;
            _anzahlPersonen = anzahlPersonen;
            _festeZuordnung = festeZuordnung;
            _kontingent = kontingent;
            Belegung = new int[wochen + 1];
            Array.Fill(Belegung, Frei);
        }

        public int[] Belegung { get; }

        public bool Abgebrochen { get; private set; }

        public bool Loese(int woche)
        {
            if (woche > _wochen)
            {
                return true;
            }

            if (++_schritte > MaxSchritte)
            {
                Abgebrochen = true;
                return false;
            }

            int vorherige = woche > 1 ? Belegung[woche - 1] : Frei;
            int naechsteFeste = woche < _wochen ? _festeZuordnung[woche + 1] : Frei;

            foreach (int person in Kandidaten(woche, vorherige, naechsteFeste))
            {
                Belegung[woche] = person;
                _kontingent[person]--;

                if (Loese(woche + 1))
                {
                    return true;
                }

                _kontingent[person]++;
                Belegung[woche] = Frei;

                if (Abgebrochen)
                {
                    return false;
                }
            }

            return false;
        }

        private IEnumerable<int> Kandidaten(int woche, int vorherige, int naechsteFeste)
        {
            int feste = _festeZuordnung[woche];
            if (feste != Frei)
            {
                // Filterwoche: die Person steht fest, es gibt nur diesen einen Kandidaten.
                if (feste != vorherige && _kontingent[feste] > 0)
                {
                    yield return feste;
                }

                yield break;
            }

            var moeglich = new List<int>(_anzahlPersonen);
            for (int person = 0; person < _anzahlPersonen; person++)
            {
                if (_kontingent[person] > 0 && person != vorherige && person != naechsteFeste)
                {
                    moeglich.Add(person);
                }
            }

            moeglich.Sort((links, rechts) =>
            {
                int nachKontingent = _kontingent[rechts].CompareTo(_kontingent[links]);
                return nachKontingent != 0 ? nachKontingent : links.CompareTo(rechts);
            });

            foreach (int person in moeglich)
            {
                yield return person;
            }
        }
    }
}
