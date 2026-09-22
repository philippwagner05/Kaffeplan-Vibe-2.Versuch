using Microsoft.Win32;

namespace Kaffeeplan.App.Infrastructure;

/// <summary>
/// Die Windows-Dateidialoge. Microsoft.Win32.OpenFileDialog und SaveFileDialog
/// gehoeren zu WPF, es kommt also keine Fremdbibliothek dazu.
/// </summary>
public sealed class DateiDialog : IDateiDialog
{
    public string? DateiZumOeffnen(string titel, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = titel,
            Filter = filter,
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? DateiZumSpeichern(string titel, string filter, string dateivorschlag)
    {
        var dialog = new SaveFileDialog
        {
            Title = titel,
            Filter = filter,
            FileName = dateivorschlag,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
