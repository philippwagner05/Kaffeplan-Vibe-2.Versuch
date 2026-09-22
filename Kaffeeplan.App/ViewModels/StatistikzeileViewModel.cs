using Kaffeeplan.Core;

namespace Kaffeeplan.App.ViewModels;

/// <summary>Eine Zeile der Statistik-Ansicht (A12).</summary>
public sealed class StatistikzeileViewModel
{
    public StatistikzeileViewModel(Mitarbeiterstatistik statistik)
    {
        ArgumentNullException.ThrowIfNull(statistik);
        Name = statistik.Mitarbeiter.Name;
        Reinigungen = statistik.Reinigungen;
        Filtertausche = statistik.Filtertausche;
    }

    public string Name { get; }

    public int Reinigungen { get; }

    public int Filtertausche { get; }
}
