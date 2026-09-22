using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kaffeeplan.Core;

public interface IDienstplanSpeicher
{
    void Speichere(Stream ziel, Speicherstand stand);

    Speicherstand Lade(Stream quelle);

    void SpeichereDatei(string pfad, Speicherstand stand);

    /// <summary>Eine fehlende Datei liefert einen leeren Stand und ist kein Fehler.</summary>
    Speicherstand LadeDatei(string pfad);
}

/// <summary>
/// Persistenz ueber System.Text.Json (A9).
/// Die Kalenderwoche wird als "2026-W39" abgelegt statt als Zahlenpaar, damit die
/// Datei von Hand lesbar bleibt. Das Versionsfeld erlaubt spaeter ein Migrieren,
/// ohne dass alte Dateien stillschweigend falsch interpretiert werden.
/// </summary>
public sealed class JsonDienstplanSpeicher : IDienstplanSpeicher
{
    public const int AktuelleVersion = 1;

    private static readonly JsonSerializerOptions Optionen = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Speichere(Stream ziel, Speicherstand stand)
    {
        ArgumentNullException.ThrowIfNull(ziel);
        ArgumentNullException.ThrowIfNull(stand);

        var dto = new SpeicherstandDto
        {
            Version = AktuelleVersion,
            Mitarbeiter = stand.Mitarbeiter
                .Select(m => new MitarbeiterDto { Id = m.Id, Name = m.Name })
                .ToList(),
            Plan = stand.Plan is null
                ? null
                : new PlanDto
                {
                    Jahr = stand.Plan.Jahr,
                    Eintraege = stand.Plan.Eintraege
                        .Select(e => new EintragDto
                        {
                            Woche = e.Woche.ToString(),
                            MitarbeiterId = e.MitarbeiterId,
                            Filtertausch = e.IstFilterwoche,
                        })
                        .ToList(),
                },
        };

        JsonSerializer.Serialize(ziel, dto, Optionen);
    }

    public Speicherstand Lade(Stream quelle)
    {
        ArgumentNullException.ThrowIfNull(quelle);

        SpeicherstandDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SpeicherstandDto>(quelle, Optionen);
        }
        catch (JsonException fehler)
        {
            throw new SpeicherAusnahme("Die Datei ist keine gültige Kaffeeplan-Datei.", fehler);
        }

        if (dto is null)
        {
            throw new SpeicherAusnahme("Die Datei ist leer.");
        }

        if (dto.Version != AktuelleVersion)
        {
            throw new SpeicherAusnahme(
                $"Die Datei hat Version {dto.Version}, dieses Programm kann Version {AktuelleVersion}.");
        }

        // Eine von Hand editierte Datei kann ueberall "null" stehen haben - auch dort,
        // wo das Schema ein Objekt oder eine Liste erwartet. System.Text.Json setzt
        // die Eigenschaft dann auf null und ueberschreibt damit auch einen Initialwert.
        // Ohne diese Pruefungen gaebe es eine NullReferenceException, die weder vom
        // catch-Filter unten noch von der Oberflaeche aufgefangen wird (A9, A11).
        if (dto.Mitarbeiter is null)
        {
            throw new SpeicherAusnahme("Die Datei enthält keine Mitarbeiterliste.");
        }

        if (dto.Mitarbeiter.Any(m => m is null))
        {
            throw new SpeicherAusnahme("Die Mitarbeiterliste in der Datei enthält einen leeren Eintrag.");
        }

        try
        {
            var mitarbeiter = dto.Mitarbeiter
                .Select(m => new Mitarbeiter(m.Id, m.Name))
                .ToList();

            // Eine von Hand editierte Datei kann dieselbe Id zweimal enthalten. Ohne
            // diese Pruefung faellt das erst spaeter auf, wenn nach Id nachgeschlagen wird.
            Mitarbeiterliste.PruefeEindeutigeIds(mitarbeiter, nameof(quelle));

            Dienstplan? plan = null;
            if (dto.Plan is not null)
            {
                if (dto.Plan.Eintraege is null)
                {
                    throw new SpeicherAusnahme("Der Plan in der Datei enthält keine Wocheneinträge.");
                }

                if (dto.Plan.Eintraege.Any(e => e is null))
                {
                    throw new SpeicherAusnahme("Der Plan in der Datei enthält einen leeren Wocheneintrag.");
                }

                var eintraege = dto.Plan.Eintraege
                    .Select(e => new Wocheneintrag(
                        Kalenderwoche.Parse(e.Woche), e.MitarbeiterId, e.Filtertausch))
                    .ToList();

                plan = new Dienstplan(dto.Plan.Jahr, eintraege);

                // Der Plan muss zur Mitarbeiterliste derselben Datei passen. Sonst
                // zeigt die Oberflaeche "Unbekannt" und die Regelpruefung wird
                // bedeutungslos, weil sie unbekannte Eintraege nicht mitzaehlt.
                var bekannteIds = mitarbeiter.Select(m => m.Id).ToHashSet();
                if (plan.Eintraege.Any(e => !bekannteIds.Contains(e.MitarbeiterId)))
                {
                    throw new SpeicherAusnahme(
                        "Der Plan in der Datei teilt Personen ein, die nicht in der Mitarbeiterliste stehen.");
                }
            }

            return new Speicherstand(mitarbeiter, plan);
        }
        catch (Exception fehler) when (fehler is ArgumentException or FormatException)
        {
            throw new SpeicherAusnahme("Der Inhalt der Datei ist nicht schlüssig.", fehler);
        }
    }

    /// <summary>Speichert in eine Datei. Ein bestehender Stand wird ersetzt.</summary>
    public void SpeichereDatei(string pfad, Speicherstand stand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pfad);

        try
        {
            using FileStream datei = File.Create(pfad);
            Speichere(datei, stand);
        }
        catch (IOException fehler)
        {
            throw new SpeicherAusnahme($"Die Datei '{pfad}' ließ sich nicht schreiben.", fehler);
        }
        catch (UnauthorizedAccessException fehler)
        {
            throw new SpeicherAusnahme($"Kein Zugriff auf die Datei '{pfad}'.", fehler);
        }
    }

    /// <summary>
    /// Laedt aus einer Datei. Eine fehlende Datei ist kein Fehler, sondern der
    /// Normalfall beim ersten Start - dann kommt ein leerer Stand zurueck.
    /// </summary>
    public Speicherstand LadeDatei(string pfad)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pfad);

        if (!File.Exists(pfad))
        {
            return Speicherstand.Leer;
        }

        try
        {
            using FileStream datei = File.OpenRead(pfad);
            return Lade(datei);
        }
        catch (IOException fehler)
        {
            throw new SpeicherAusnahme($"Die Datei '{pfad}' ließ sich nicht lesen.", fehler);
        }
        catch (UnauthorizedAccessException fehler)
        {
            throw new SpeicherAusnahme($"Kein Zugriff auf die Datei '{pfad}'.", fehler);
        }
    }

    private sealed class SpeicherstandDto
    {
        public int Version { get; set; }

        public List<MitarbeiterDto> Mitarbeiter { get; set; } = [];

        public PlanDto? Plan { get; set; }
    }

    private sealed class MitarbeiterDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class PlanDto
    {
        public int Jahr { get; set; }

        public List<EintragDto> Eintraege { get; set; } = [];
    }

    private sealed class EintragDto
    {
        public string Woche { get; set; } = string.Empty;

        public Guid MitarbeiterId { get; set; }

        public bool Filtertausch { get; set; }
    }
}
