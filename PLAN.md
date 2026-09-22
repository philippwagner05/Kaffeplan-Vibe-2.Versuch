# Implementierungsplan — Kaffeemaschinen-Dienstplan (Stufe 2)

**Grundlage:** `SPEC.md` (v2, 2026-09-11) und `claude.md`
**Stand:** 2026-09-22
**Status:** abgestimmt, Umsetzung noch nicht begonnen

---

## 0. Der eigentliche Knackpunkt

Vorab, weil er die ganze Klassenstruktur bestimmt: **naives Reihum erfüllt F2 nicht.**
Bei 6 Personen und `Person = (KW-1) mod 6` landen die Filterwochen 1, 9, 17, 25, 33,
41, 49 auf den Indizes 0, 2, 4, 0, 2, 4, 0 — drei Personen bekommen 3/2/2
Filtertausche, die anderen drei **null**. Differenz 3, F2 verletzt. Genau die Falle,
die SPEC 1.4 beschreibt.

Der gewählte Ansatz: **Filterwochen zuerst**, Rest danach.

1. Die 7 Filterwochen reihum auf *verschiedene* Personen verteilen
   (`Filterwoche k → Person k mod N`) → F2 gilt per Konstruktion.
2. Die übrigen Wochen mit einem Greedy über Restkontingente füllen: pro Woche die
   Person mit dem größten Restkontingent wählen, die nicht in der Vorwoche dran war.
3. Wenn der Greedy sich festfährt: Backtracking mit Schrittlimit.

Damit sind F1, F2 und F3 gleichzeitig erfüllbar — und der Planer meldet sauber, wenn
es *keine* Lösung gibt. Das ist kein hypothetischer Fall:

- **N = 2 ist nachweislich unlösbar.** F3 erzwingt strikte Abwechslung, alle
  Filterwochen (1, 9, 17, …) sind ungerade, treffen also immer dieselbe Person → F2
  unmöglich.
- **N = 1 verletzt F3 zwangsläufig.**

Beides muss laut A11 ohne Absturz durchgehen → der Planer liefert ein
`PlanungsErgebnis` mit Fehlermeldung statt einer Exception.

---

## 1. Projektstruktur

| Projekt | Typ | TFM | Referenzen |
|---|---|---|---|
| `Kaffeeplan.Core` | classlib | `net10.0` | — |
| `Kaffeeplan.App` | WPF | `net10.0-windows` | Core |
| `Kaffeeplan.Tests` | MSTest | `net10.0-windows` | Core, App |

`Kaffeeplan.Tests` läuft auf `net10.0-windows`, weil A8 und A12 ViewModels betreffen
und „keine Funktion ohne Test" gilt. Ausschließlich SDK-Pakete
(`Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework`), sonst kein
NuGet.

---

## 2. Klassen in `Kaffeeplan.Core`

### Fachmodell — deutsche Bezeichner

| Klasse | Inhalt |
|---|---|
| `Kalenderwoche` | `readonly record struct` (`Jahr`, `Woche`). Kapselt `ISOWeek` **vollständig** — sonst ruft niemand `ISOWeek` direkt auf. `Montag`, `Sonntag`, `Naechste()`, `IComparable`, `WochenImJahr(jahr)`, Validierung gegen `ISOWeek.GetWeeksInYear`. |
| `Mitarbeiter` | `record` (`Id`, `Name`). Name darf nicht leer sein. |
| `Wocheneintrag` | `Kalenderwoche`, `MitarbeiterId`, `IstFilterwoche` |
| `Dienstplan` | `Jahr` + `IReadOnlyList<Wocheneintrag>`. Invariante: genau ein Eintrag je KW des Jahres (A2). |
| `Filterrhythmus` | `Startwoche = 1`, `Intervall = 8`. `IstFilterwoche(int)`, `Filterwochen(jahr)`. Konfigurierbar statt hartkodiert. |
| `Dienstplaner` | `Erzeuge(jahr, IReadOnlyList<Mitarbeiter>) → PlanungsErgebnis`. Der Algorithmus aus Abschnitt 0. Rein, kein I/O, deterministisch. |
| `PlanungsErgebnis` | `Erfolgreich`, `Dienstplan?`, `Fehlermeldung` — kein Exception-Flow für erwartbare Fälle (A11). |
| `Regelpruefer` | `PruefeF1/F2/F3(plan, mitarbeiter)`. Von Tests **und** UI genutzt — macht A4–A6 zu Einzeilern und den Plan zur Laufzeit überprüfbar. |
| `Statistikrechner`, `Mitarbeiterstatistik` | Reinigungen und Filtertausche pro Person (A12) |

### Technik — englische Bezeichner

| Klasse | Zweck |
|---|---|
| `IDienstplanSpeicher`, `JsonDienstplanSpeicher` | `System.Text.Json`, DTO `SpeicherstandDto` (Mitarbeiter **und** Plan, A9) mit Versionsfeld |
| `KalenderwocheJsonConverter` | Format `"2026-W39"`, damit die Datei lesbar bleibt |
| `CsvExporter` | Semikolon-getrennt mit UTF-8-BOM → öffnet in deutschem Excel korrekt (A10). Schreibt in einen `TextWriter`, dadurch ohne Datei testbar. |
| `IClock`, `SystemClock` | „aktuelles Jahr" als Vorbelegung, testbar |

---

## 3. Klassen in `Kaffeeplan.App`

- `App.xaml.cs` — Composition Root, baut alles von Hand zusammen (kein DI-Container,
  da das ein NuGet-Paket wäre)
- `MainWindow.xaml` — Jahr-Auswahl, Mitarbeiterliste, Planliste, Statistik-Panel,
  Buttons. Code-Behind nur `InitializeComponent()`.
- `ViewModels/MainViewModel` — Jahr, Mitarbeiter, Plan, Statistik, Status-Meldung;
  Kommandos `PlanErzeugen`, `MitarbeiterHinzufuegen`, `MitarbeiterEntfernen`,
  `Speichern`, `Laden`, `CsvExportieren`
- `ViewModels/MitarbeiterViewModel`, `WochenzeileViewModel` (KW, Datumsbereich, Name,
  Filter-Markierung), `StatistikzeileViewModel`
- `Infrastructure/ObservableObject`, `RelayCommand` — selbst geschrieben, ersetzt
  CommunityToolkit.Mvvm
- `IDateiDialog`, `DateiDialog` — dünne Hülle um `Microsoft.Win32.*FileDialog` (in WPF
  enthalten), damit das ViewModel ohne UI testbar bleibt

---

## 4. Tests und ihre Zuordnung zu den Akzeptanzkriterien

| Testklasse | Fälle | deckt ab |
|---|---|---|
| `KalenderwocheTests` | Jahreswechsel (29.12.2025 → KW 1/2026), 53-Wochen-Jahre 2026 und 2032, Montag/Sonntag, `Naechste()` über die Jahresgrenze, ungültige Wochennummer | A7 |
| `MitarbeiterTests` | Gleichheit, leerer Name | — |
| `FilterrhythmusTests` | exakt 1, 9, 17, 25, 33, 41, 49; genau 7 Termine; auch bei 53 Wochen 7 | A3 |
| `DienstplanTests` | genau ein Eintrag je KW, keine Lücke, keine Dublette | A2 |
| `RegelprueferTests` | erkennt *konstruierte* Verstöße gegen F1/F2/F3 — der Prüfer muss getestet sein, bevor mit ihm der Planer geprüft wird | — |
| `DienstplanerTests` | **Kern:** für N = 4…8 × Jahre 2025/2026/2027/2032 jeweils F1, F2, F3 via `Regelpruefer`; explizit die 6/52-Erwartung aus SPEC 1.4 (4 × 9 + 2 × 8, ein Filter 2× und fünf 1×); Determinismus (zweimal gleicher Input → gleicher Plan); N = 0/1/2 → `Erfolgreich == false` mit Meldung statt Absturz | A4, A5, A6, A11 |
| `StatistikrechnerTests` | Summe Reinigungen = Wochenzahl, Summe Filtertausche = 7 | A12 |
| `JsonDienstplanSpeicherTests` | Round-Trip über `MemoryStream`, Wochenformat in der Datei, fehlende und kaputte Datei, unbekannte Version | A9 |
| `CsvExporterTests` | Spalten, Trennzeichen, Escaping bei Semikolon im Namen, BOM | A10 |
| `MainViewModelTests` | Hinzufügen/Entfernen aktualisiert die Liste; `CanExecute` bei leerer Liste; unlösbarer Fall setzt die Meldung ohne Exception; Speichern ruft den Fake-Speicher | A8, A11 |

A1 bleibt Vorführung (WPF-Oberfläche, kein UI-Test).

---

## 5. Reihenfolge

Jeder Schritt endet mit `dotnet build` und `dotnet test`; das Ergebnis wird gezeigt.

| # | Schritt | warum hier |
|---|---|---|
| 1 | Solution, drei Projekte, Referenzen, ein Smoke-Test | beweist, dass .NET 10, WPF und MSTest hier bauen |
| 2 | `Kalenderwoche` + Tests | Fundament, und der Teil mit den meisten Fallstricken (A7) |
| 3 | `Mitarbeiter`, `Filterrhythmus`, `Wocheneintrag`, `Dienstplan` + Tests | A2, A3 |
| 4 | `Regelpruefer` + Tests | **vor** dem Planer — sonst wird der Algorithmus mit ungeprüftem Werkzeug geprüft |
| 5 | `Dienstplaner` + Tests | das Herzstück; ab hier sind A4–A6 nachgewiesen |
| 6 | `Statistikrechner` + Tests | Datengrundlage für A12 |
| 7 | `JsonDienstplanSpeicher`, `CsvExporter` + Tests | A9, A10 — Core ist damit fertig und vollständig getestet |
| 8 | `MainViewModel` + Tests gegen Fakes | **vor** der View, damit keine Logik ins Code-Behind rutscht |
| 9 | `MainWindow.xaml`, Bindings, `App.xaml.cs` | A1, A8, A12 vorführbar |
| 10 | Durchlauf aller Akzeptanzkriterien, Kurzprotokoll | Abnahme |

Schritt 4 vor Schritt 5 ist die einzige nicht offensichtliche Reihenfolge und
bewusst so gewählt.

---

## 6. Annahmen

Diese Punkte legt `SPEC.md` nicht fest; so werden sie umgesetzt, solange nichts
anderes vereinbart wird.

1. **F3 endet am Jahreswechsel.** Jedes Jahr wird eigenständig geplant. Ob die letzte
   KW des einen und die KW 1 des nächsten Jahres dieselbe Person treffen, wird nicht
   geprüft.
2. **N = 2 gilt als unlösbar** (Begründung in Abschnitt 0). Der Planer meldet das,
   statt einen F2-verletzenden Plan zu liefern.
3. **Die Reihenfolge der Mitarbeiterliste bestimmt den Start der Filterverteilung.**
   Kein Zufall, keine Durchmischung — gleicher Input ergibt immer denselben Plan.
   Das ist leichter vorführbar und testbar.
4. **CSV mit Semikolon und UTF-8-BOM**, weil A10 ausdrücklich „in Excel öffnen"
   verlangt und deutsches Excel bei Komma alles in eine Spalte packt.

---

## 7. Arbeitsgrenze

Gelesen und geschrieben wird ausschließlich in `Stage2-claude-code`. Fehlt eine
Information, wird nachgefragt, statt außerhalb des Ordners zu suchen.
