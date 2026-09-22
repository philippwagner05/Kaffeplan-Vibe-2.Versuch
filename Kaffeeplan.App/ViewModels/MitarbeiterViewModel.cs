using Kaffeeplan.App.Infrastructure;
using Kaffeeplan.Core;

namespace Kaffeeplan.App.ViewModels;

/// <summary>Eine Zeile der Mitarbeiterliste. Der Name ist aenderbar, die Id nicht.</summary>
public sealed class MitarbeiterViewModel : ObservableObject
{
    private string _name;

    public MitarbeiterViewModel(Mitarbeiter mitarbeiter)
    {
        ArgumentNullException.ThrowIfNull(mitarbeiter);
        Id = mitarbeiter.Id;
        _name = mitarbeiter.Name;
    }

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            // Ein leerer Name wird still verworfen: die DataGrid-Zelle faellt damit auf
            // den alten Wert zurueck, statt eine unbenannte Person zu hinterlassen.
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetProperty(ref _name, value.Trim());
            }
            else
            {
                RaisePropertyChanged();
            }
        }
    }

    public Mitarbeiter AlsModell() => new(Id, Name);
}
