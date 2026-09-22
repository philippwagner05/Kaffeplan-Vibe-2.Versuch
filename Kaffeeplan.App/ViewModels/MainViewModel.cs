using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Kaffeeplan.App.Infrastructure;
using Kaffeeplan.Core;

namespace Kaffeeplan.App.ViewModels;

/// <summary>
/// Das ViewModel des Hauptfensters. Die gesamte Ablaufsteuerung liegt hier, das
/// Code-Behind von MainWindow enthaelt nur InitializeComponent.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private const string JsonFilter = "Kaffeeplan-Dateien (*.json)|*.json|Alle Dateien (*.*)|*.*";
    private const string CsvFilter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";

    private readonly Dienstplaner _planer;
    private readonly IDienstplanSpeicher _speicher;
    private readonly IDateiDialog _dialog;
    private readonly ICsvExport _csvExport;

    private string _jahrEingabe;
    private string _neuerMitarbeiterName = string.Empty;
    private string _statusMeldung = string.Empty;
    private string _regelstatus = string.Empty;
    private MitarbeiterViewModel? _ausgewaehlterMitarbeiter;
    private Dienstplan? _plan;

    public MainViewModel(
        Dienstplaner planer,
        IDienstplanSpeicher speicher,
        IDateiDialog dialog,
        ICsvExport csvExport,
        IClock uhr,
        IEnumerable<Mitarbeiter>? startteam = null)
    {
        ArgumentNullException.ThrowIfNull(planer);
        ArgumentNullException.ThrowIfNull(speicher);
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(csvExport);
        ArgumentNullException.ThrowIfNull(uhr);

        _planer = planer;
        _speicher = speicher;
        _dialog = dialog;
        _csvExport = csvExport;
        _jahrEingabe = uhr.Heute.Year.ToString(CultureInfo.InvariantCulture);

        PlanErzeugenCommand = new RelayCommand(PlanErzeugen, () => Mitarbeiter.Count > 0);
        MitarbeiterHinzufuegenCommand = new RelayCommand(
            MitarbeiterHinzufuegen, () => !string.IsNullOrWhiteSpace(NeuerMitarbeiterName));
        MitarbeiterEntfernenCommand = new RelayCommand(
            MitarbeiterEntfernen, () => AusgewaehlterMitarbeiter is not null);
        JahrZurueckCommand = new RelayCommand(() => VerschiebeJahr(-1));
        JahrVorCommand = new RelayCommand(() => VerschiebeJahr(+1));
        SpeichernCommand = new RelayCommand(Speichern, () => Mitarbeiter.Count > 0);
        LadenCommand = new RelayCommand(Laden);
        CsvExportierenCommand = new RelayCommand(CsvExportieren, () => _plan is not null);

        Mitarbeiter.CollectionChanged += (_, _) => MitarbeiterlisteGeaendert();

        foreach (Mitarbeiter person in startteam ?? [])
        {
            Fuegehinzu(new MitarbeiterViewModel(person));
        }
    }

    public ObservableCollection<MitarbeiterViewModel> Mitarbeiter { get; } = [];

    public ObservableCollection<WochenzeileViewModel> Wochen { get; } = [];

    public ObservableCollection<StatistikzeileViewModel> Statistik { get; } = [];

    public RelayCommand PlanErzeugenCommand { get; }

    public RelayCommand MitarbeiterHinzufuegenCommand { get; }

    public RelayCommand MitarbeiterEntfernenCommand { get; }

    public RelayCommand JahrZurueckCommand { get; }

    public RelayCommand JahrVorCommand { get; }

    public RelayCommand SpeichernCommand { get; }

    public RelayCommand LadenCommand { get; }

    public RelayCommand CsvExportierenCommand { get; }

    public string JahrEingabe
    {
        get => _jahrEingabe;
        set => SetProperty(ref _jahrEingabe, value);
    }

    public string NeuerMitarbeiterName
    {
        get => _neuerMitarbeiterName;
        set
        {
            if (SetProperty(ref _neuerMitarbeiterName, value))
            {
                MitarbeiterHinzufuegenCommand.MeldeAenderung();
            }
        }
    }

    public MitarbeiterViewModel? AusgewaehlterMitarbeiter
    {
        get => _ausgewaehlterMitarbeiter;
        set
        {
            if (SetProperty(ref _ausgewaehlterMitarbeiter, value))
            {
                MitarbeiterEntfernenCommand.MeldeAenderung();
            }
        }
    }

    /// <summary>Rueckmeldung an den Benutzer - Erfolg wie Fehler, nie eine Exception ins Leere.</summary>
    public string StatusMeldung
    {
        get => _statusMeldung;
        private set => SetProperty(ref _statusMeldung, value);
    }

    /// <summary>Das Ergebnis der Pruefung gegen F1, F2 und F3 zum angezeigten Plan.</summary>
    public string Regelstatus
    {
        get => _regelstatus;
        private set => SetProperty(ref _regelstatus, value);
    }

    public bool HatPlan => _plan is not null;

    public Dienstplan? Plan => _plan;

    // ----- Plan -----

    private void PlanErzeugen()
    {
        if (!TryLiesJahr(out int jahr))
        {
            return;
        }

        List<Mitarbeiter> team = Team();
        PlanungsErgebnis ergebnis = _planer.Erzeuge(jahr, team);

        if (!ergebnis.Erfolgreich)
        {
            SetzePlan(null, team);
            StatusMeldung = ergebnis.Fehlermeldung!;
            return;
        }

        SetzePlan(ergebnis.Plan, team);
        StatusMeldung = $"Plan für {jahr} erzeugt: {ergebnis.Plan!.Wochenanzahl} Kalenderwochen, " +
                        $"{ergebnis.Plan.Filterwochen.Count()} Filtertermine.";
    }

    private void SetzePlan(Dienstplan? plan, IReadOnlyList<Mitarbeiter> team)
    {
        _plan = plan;

        Wochen.Clear();
        Statistik.Clear();

        if (plan is not null)
        {
            var namen = team.ToDictionary(m => m.Id, m => m.Name);
            foreach (Wocheneintrag eintrag in plan.Eintraege)
            {
                string name = namen.TryGetValue(eintrag.MitarbeiterId, out string? gefunden)
                    ? gefunden
                    : "Unbekannt";

                Wochen.Add(new WochenzeileViewModel(eintrag, name));
            }

            foreach (Mitarbeiterstatistik zeile in Statistikrechner.Berechne(plan, team))
            {
                Statistik.Add(new StatistikzeileViewModel(zeile));
            }

            Pruefergebnis geprueft = Regelpruefer.Pruefe(plan, team);
            Regelstatus = geprueft.AllesErfuellt
                ? "F1, F2 und F3 erfüllt."
                : geprueft.ToString();
        }
        else
        {
            Regelstatus = string.Empty;
        }

        RaisePropertyChanged(nameof(HatPlan));
        RaisePropertyChanged(nameof(Plan));
        CsvExportierenCommand.MeldeAenderung();
    }

    private bool TryLiesJahr(out int jahr)
    {
        if (!int.TryParse(JahrEingabe, NumberStyles.Integer, CultureInfo.InvariantCulture, out jahr) ||
            jahr < Kalenderwoche.MinJahr || jahr > Kalenderwoche.MaxJahr)
        {
            StatusMeldung = $"'{JahrEingabe}' ist keine gültige Jahreszahl " +
                            $"({Kalenderwoche.MinJahr} bis {Kalenderwoche.MaxJahr}).";
            return false;
        }

        return true;
    }

    private void VerschiebeJahr(int schritt)
    {
        if (!int.TryParse(JahrEingabe, NumberStyles.Integer, CultureInfo.InvariantCulture, out int jahr))
        {
            StatusMeldung = $"'{JahrEingabe}' ist keine gültige Jahreszahl.";
            return;
        }

        int neu = jahr + schritt;
        if (neu < Kalenderwoche.MinJahr || neu > Kalenderwoche.MaxJahr)
        {
            return;
        }

        JahrEingabe = neu.ToString(CultureInfo.InvariantCulture);
        if (HatPlan)
        {
            PlanErzeugen();
        }
    }

    // ----- Mitarbeiter -----

    private void MitarbeiterHinzufuegen()
    {
        string name = NeuerMitarbeiterName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        bool hatteVorherPlan = HatPlan;

        Fuegehinzu(new MitarbeiterViewModel(new Mitarbeiter(name)));
        NeuerMitarbeiterName = string.Empty;
        MeldeTeamaenderung($"{name} wurde hinzugefügt.", hatteVorherPlan);
    }

    private void MitarbeiterEntfernen()
    {
        if (AusgewaehlterMitarbeiter is not { } person)
        {
            return;
        }

        bool hatteVorherPlan = HatPlan;

        person.PropertyChanged -= MitarbeiterGeaendert;
        Mitarbeiter.Remove(person);
        AusgewaehlterMitarbeiter = null;
        MeldeTeamaenderung($"{person.Name} wurde entfernt.", hatteVorherPlan);
    }

    /// <summary>
    /// Die Teamaenderung hat den Plan bereits neu berechnet. Ist er dabei weggefallen,
    /// bleibt die Erklaerung des Planers stehen - sonst verschwindet die Wochenliste
    /// kommentarlos und der Benutzer sieht nur "X wurde entfernt".
    /// </summary>
    private void MeldeTeamaenderung(string meldung, bool hatteVorherPlan)
    {
        if (hatteVorherPlan && !HatPlan)
        {
            return;
        }

        StatusMeldung = meldung;
    }

    private void Fuegehinzu(MitarbeiterViewModel person)
    {
        person.PropertyChanged += MitarbeiterGeaendert;
        Mitarbeiter.Add(person);
    }

    private void MitarbeiterGeaendert(object? absender, PropertyChangedEventArgs e) =>
        NeuBerechnenFallsPlanVorhanden();

    private void MitarbeiterlisteGeaendert()
    {
        PlanErzeugenCommand.MeldeAenderung();
        SpeichernCommand.MeldeAenderung();
        NeuBerechnenFallsPlanVorhanden();
    }

    /// <summary>
    /// Aendert sich das Team, waere ein bestehender Plan veraltet. Statt ihn stehen zu
    /// lassen, wird er neu erzeugt - so zeigt die Oberflaeche nie eine Einteilung, die
    /// zur aktuellen Mannschaft nicht mehr passt.
    /// </summary>
    private void NeuBerechnenFallsPlanVorhanden()
    {
        if (HatPlan)
        {
            PlanErzeugen();
        }
    }

    private List<Mitarbeiter> Team() => Mitarbeiter.Select(m => m.AlsModell()).ToList();

    // ----- Speichern, Laden, Export -----

    private void Speichern()
    {
        string? pfad = _dialog.DateiZumSpeichern("Kaffeeplan speichern", JsonFilter, "kaffeeplan.json");
        if (pfad is null)
        {
            return;
        }

        try
        {
            _speicher.SpeichereDatei(pfad, new Speicherstand(Team(), _plan));
            StatusMeldung = $"Gespeichert: {pfad}";
        }
        catch (SpeicherAusnahme fehler)
        {
            StatusMeldung = fehler.Message;
        }
    }

    private void Laden()
    {
        string? pfad = _dialog.DateiZumOeffnen("Kaffeeplan laden", JsonFilter);
        if (pfad is null)
        {
            return;
        }

        Speicherstand stand;
        try
        {
            stand = _speicher.LadeDatei(pfad);
        }
        catch (SpeicherAusnahme fehler)
        {
            StatusMeldung = fehler.Message;
            return;
        }

        // Der Stand kommt von aussen und kann eine Person doppelt enthalten. Das wird
        // geprueft, bevor irgendetwas uebernommen wird - sonst bleibt die Oberflaeche
        // halb umgestellt zurueck.
        if (!Mitarbeiterliste.HatEindeutigeIds(stand.Mitarbeiter))
        {
            StatusMeldung = $"{Mitarbeiterliste.DublettenMeldung} Die Datei wurde nicht geladen.";
            return;
        }

        UebernimmStand(stand);
        StatusMeldung = stand.Plan is null
            ? $"Geladen: {stand.Mitarbeiter.Count} Mitarbeiter, kein Plan in der Datei."
            : $"Geladen: {stand.Mitarbeiter.Count} Mitarbeiter, Plan für {stand.Plan.Jahr}.";
    }

    private void UebernimmStand(Speicherstand stand)
    {
        foreach (MitarbeiterViewModel alt in Mitarbeiter)
        {
            alt.PropertyChanged -= MitarbeiterGeaendert;
        }

        // Waehrend des Austauschs darf der Plan nicht neu berechnet werden, sonst
        // ueberschreibt eine Zwischenberechnung den gerade geladenen Plan.
        _plan = null;
        Mitarbeiter.Clear();

        foreach (Mitarbeiter person in stand.Mitarbeiter)
        {
            Fuegehinzu(new MitarbeiterViewModel(person));
        }

        AusgewaehlterMitarbeiter = null;

        if (stand.Plan is not null)
        {
            JahrEingabe = stand.Plan.Jahr.ToString(CultureInfo.InvariantCulture);
        }

        SetzePlan(stand.Plan, stand.Mitarbeiter);
    }

    private void CsvExportieren()
    {
        if (_plan is null)
        {
            return;
        }

        string? pfad = _dialog.DateiZumSpeichern(
            "Plan als CSV exportieren", CsvFilter, $"kaffeeplan-{_plan.Jahr}.csv");

        if (pfad is null)
        {
            return;
        }

        try
        {
            _csvExport.Exportiere(pfad, _plan, Team());
            StatusMeldung = $"CSV geschrieben: {pfad}";
        }
        catch (Exception fehler) when (fehler is IOException or UnauthorizedAccessException)
        {
            StatusMeldung = $"Die Datei ließ sich nicht schreiben: {fehler.Message}";
        }
    }
}
