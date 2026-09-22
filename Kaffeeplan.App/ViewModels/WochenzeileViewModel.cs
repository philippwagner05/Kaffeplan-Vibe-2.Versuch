using System.Globalization;
using Kaffeeplan.Core;

namespace Kaffeeplan.App.ViewModels;

/// <summary>
/// Eine Zeile im Plan-DataGrid. <see cref="IstFilterwoche"/> steuert die farbliche
/// Hervorhebung ueber einen DataTrigger im XAML.
/// </summary>
public sealed class WochenzeileViewModel
{
    public WochenzeileViewModel(Wocheneintrag eintrag, string name)
    {
        Kalenderwoche = eintrag.Woche.Woche;
        Von = eintrag.Woche.Montag;
        Bis = eintrag.Woche.Sonntag;
        Name = name;
        IstFilterwoche = eintrag.IstFilterwoche;
    }

    public int Kalenderwoche { get; }

    public DateOnly Von { get; }

    public DateOnly Bis { get; }

    public string Zeitraum =>
        $"{Von.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"))} - " +
        $"{Bis.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"))}";

    public string Name { get; }

    public bool IstFilterwoche { get; }

    public string Aufgabe => IstFilterwoche ? "Reinigung + Filtertausch" : "Reinigung";
}
