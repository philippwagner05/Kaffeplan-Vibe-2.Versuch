using System.Windows;
using Kaffeeplan.App.Infrastructure;
using Kaffeeplan.App.ViewModels;
using Kaffeeplan.Core;

namespace Kaffeeplan.App;

/// <summary>
/// Composition Root: hier und nur hier werden die Bausteine zusammengesteckt.
/// Von Hand statt ueber einen DI-Container, weil jeder Container eine
/// Fremdbibliothek waere.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Vorbelegung der Mitarbeiterliste aus der Aufgabenstellung. Das ist ein
    /// Startwert, keine Festverdrahtung: die Liste laesst sich in der Oberflaeche
    /// vollstaendig aendern und mit jeder anderen Groesse verwenden.
    /// </summary>
    private static readonly string[] Startteam =
        ["Ralf", "Jochen", "Mario", "Gabriel", "Ehsan", "Shariyar"];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = new MainViewModel(
            new Dienstplaner(),
            new JsonDienstplanSpeicher(),
            new DateiDialog(),
            new CsvDateiExport(),
            SystemClock.Instanz,
            Startteam.Select(name => new Mitarbeiter(name)));

        var fenster = new MainWindow { DataContext = viewModel };
        MainWindow = fenster;
        fenster.Show();
    }
}
