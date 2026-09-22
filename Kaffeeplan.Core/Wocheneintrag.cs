namespace Kaffeeplan.Core;

/// <summary>
/// Eine Kalenderwoche und die dafuer zustaendige Person.
/// Faellt in dieser Woche zusaetzlich der Filtertausch an, ist
/// <see cref="IstFilterwoche"/> gesetzt - erledigt wird er von derselben Person,
/// es wird also keine zweite eingeteilt.
/// </summary>
public sealed record Wocheneintrag
{
    public Wocheneintrag(Kalenderwoche woche, Guid mitarbeiterId, bool istFilterwoche)
    {
        if (mitarbeiterId == Guid.Empty)
        {
            throw new ArgumentException("Die Mitarbeiter-Id darf nicht leer sein.", nameof(mitarbeiterId));
        }

        Woche = woche;
        MitarbeiterId = mitarbeiterId;
        IstFilterwoche = istFilterwoche;
    }

    public Kalenderwoche Woche { get; }

    public Guid MitarbeiterId { get; }

    public bool IstFilterwoche { get; }
}
