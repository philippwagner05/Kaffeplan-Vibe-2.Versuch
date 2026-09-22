namespace Kaffeeplan.Core;

/// <summary>
/// Zugriff auf das heutige Datum. Als Schnittstelle, damit sich "welches Jahr ist
/// vorbelegt" testen laesst, ohne dass der Test am Kalender des Rechners haengt.
/// </summary>
public interface IClock
{
    DateOnly Heute { get; }
}

/// <summary>Die Systemuhr.</summary>
public sealed class SystemClock : IClock
{
    public static SystemClock Instanz { get; } = new();

    public DateOnly Heute => DateOnly.FromDateTime(DateTime.Now);
}
