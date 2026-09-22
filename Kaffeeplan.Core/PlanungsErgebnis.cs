namespace Kaffeeplan.Core;

/// <summary>
/// Ergebnis eines Planungslaufs. Bewusst kein Exception-Flow: dass fuer ein Team
/// kein gueltiger Plan existiert (etwa bei einer oder zwei Personen), ist ein
/// erwartbarer Fall und keine Ausnahmesituation. Die Oberflaeche zeigt dann die
/// Meldung an, statt abzustuerzen (A11).
/// </summary>
public sealed class PlanungsErgebnis
{
    private PlanungsErgebnis(Dienstplan? plan, string? fehlermeldung)
    {
        Plan = plan;
        Fehlermeldung = fehlermeldung;
    }

    public static PlanungsErgebnis Erfolg(Dienstplan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new PlanungsErgebnis(plan, null);
    }

    public static PlanungsErgebnis Fehler(string meldung)
    {
        if (string.IsNullOrWhiteSpace(meldung))
        {
            throw new ArgumentException("Eine Fehlermeldung darf nicht leer sein.", nameof(meldung));
        }

        return new PlanungsErgebnis(null, meldung);
    }

    public bool Erfolgreich => Plan is not null;

    public Dienstplan? Plan { get; }

    public string? Fehlermeldung { get; }

    public override string ToString() => Erfolgreich ? $"Plan fuer {Plan!.Jahr}" : Fehlermeldung!;
}
