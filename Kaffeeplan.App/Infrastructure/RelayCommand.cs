using System.Windows.Input;

namespace Kaffeeplan.App.Infrastructure;

/// <summary>
/// Ein Kommando aus Delegaten. Die Neubewertung von CanExecute wird bewusst
/// ausdruecklich angestossen (<see cref="MeldeAenderung"/>) statt ueber den
/// CommandManager - so verhaelt sich das ViewModel im Test genauso wie in der
/// Oberflaeche und nicht abhaengig von einer laufenden WPF-Nachrichtenschleife.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _ausfuehren;
    private readonly Func<bool>? _kannAusfuehren;

    public RelayCommand(Action ausfuehren, Func<bool>? kannAusfuehren = null)
    {
        ArgumentNullException.ThrowIfNull(ausfuehren);
        _ausfuehren = ausfuehren;
        _kannAusfuehren = kannAusfuehren;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _kannAusfuehren is null || _kannAusfuehren();

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _ausfuehren();
        }
    }

    public void MeldeAenderung() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
