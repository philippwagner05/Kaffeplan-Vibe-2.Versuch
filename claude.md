# Kaffeemaschinen-Dienstplan

## Technische Vorgaben
- C#, .NET 10, WPF, MSTest
- KEINE NuGet-Pakete außer dem, was das SDK mitbringt
- Kalenderwochen ausschließlich über System.Globalization.ISOWeek (ISO 8601)
- Persistenz über System.Text.Json

## Projektstruktur
- Kaffeeplan.Core   Fachlogik, kennt keine UI
- Kaffeeplan.App    WPF, MVVM, Logik im ViewModel statt im Code-Behind
- Kaffeeplan.Tests  MSTest

## Arbeitsweise
- Nach jeder Änderung `dotnet build` und `dotnet test` ausführen und das Ergebnis zeigen
- Keine Funktion ohne Test
- Deutsche Bezeichner im Fachmodell, englische in technischen Hilfsklassen
