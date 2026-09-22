namespace Kaffeeplan.Core;

/// <summary>
/// Eine Person, die fuer die Kaffeemaschine eingeteilt werden kann.
/// Die Identitaet haengt an der <see cref="Id"/>, nicht am Namen - ein Mitarbeiter
/// darf umbenannt werden, ohne dass ein gespeicherter Plan ungueltig wird.
/// </summary>
public sealed record Mitarbeiter
{
    public Mitarbeiter(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Die Id darf nicht leer sein.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Der Name darf nicht leer sein.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
    }

    public Mitarbeiter(string name) : this(Guid.NewGuid(), name)
    {
    }

    public Guid Id { get; }

    public string Name { get; }

    public override string ToString() => Name;
}
