using Kaffeeplan.App.Infrastructure;
using Kaffeeplan.Core;

namespace Kaffeeplan.Tests;

[TestClass]
public sealed class RelayCommandTests
{
    [TestMethod]
    public void Ein_Kommando_ohne_Bedingung_ist_immer_ausfuehrbar()
    {
        int laeufe = 0;
        var kommando = new RelayCommand(() => laeufe++);

        Assert.IsTrue(kommando.CanExecute(null));
        kommando.Execute(null);

        Assert.AreEqual(1, laeufe);
    }

    [TestMethod]
    public void Ein_gesperrtes_Kommando_wird_nicht_ausgefuehrt()
    {
        int laeufe = 0;
        bool erlaubt = false;
        var kommando = new RelayCommand(() => laeufe++, () => erlaubt);

        kommando.Execute(null);
        Assert.AreEqual(0, laeufe);

        erlaubt = true;
        kommando.Execute(null);
        Assert.AreEqual(1, laeufe);
    }

    [TestMethod]
    public void MeldeAenderung_loest_CanExecuteChanged_aus()
    {
        var kommando = new RelayCommand(() => { });
        int meldungen = 0;
        kommando.CanExecuteChanged += (_, _) => meldungen++;

        kommando.MeldeAenderung();

        Assert.AreEqual(1, meldungen);
    }

    [TestMethod]
    public void Ohne_Aktion_laesst_sich_kein_Kommando_bauen()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RelayCommand(null!));
    }
}

[TestClass]
public sealed class ObservableObjectTests
{
    private sealed class Probe : ObservableObject
    {
        private string _wert = string.Empty;

        public string Wert
        {
            get => _wert;
            set => SetProperty(ref _wert, value);
        }
    }

    [TestMethod]
    public void Ein_geaenderter_Wert_wird_gemeldet()
    {
        var probe = new Probe();
        var gemeldet = new List<string?>();
        probe.PropertyChanged += (_, e) => gemeldet.Add(e.PropertyName);

        probe.Wert = "neu";

        CollectionAssert.AreEqual(new[] { nameof(Probe.Wert) }, gemeldet);
    }

    [TestMethod]
    public void Ein_unveraenderter_Wert_wird_nicht_gemeldet()
    {
        var probe = new Probe { Wert = "gleich" };
        var gemeldet = new List<string?>();
        probe.PropertyChanged += (_, e) => gemeldet.Add(e.PropertyName);

        probe.Wert = "gleich";

        Assert.IsEmpty(gemeldet);
    }
}

[TestClass]
public sealed class SystemClockTests
{
    [TestMethod]
    public void Die_Systemuhr_liefert_das_heutige_Datum()
    {
        Assert.AreEqual(DateOnly.FromDateTime(DateTime.Now), SystemClock.Instanz.Heute);
    }
}
