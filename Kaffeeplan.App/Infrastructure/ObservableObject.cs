using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kaffeeplan.App.Infrastructure;

/// <summary>
/// Basisklasse fuer ViewModels. Ersetzt CommunityToolkit.Mvvm, das als
/// Fremdbibliothek ausscheidet.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? eigenschaft = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(eigenschaft));

    /// <summary>Setzt das Feld und meldet die Aenderung, sofern sich der Wert geaendert hat.</summary>
    protected bool SetProperty<T>(ref T feld, T wert, [CallerMemberName] string? eigenschaft = null)
    {
        if (EqualityComparer<T>.Default.Equals(feld, wert))
        {
            return false;
        }

        feld = wert;
        RaisePropertyChanged(eigenschaft);
        return true;
    }
}
