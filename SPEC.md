# Aufgabenstellung: Dienstplan für die Kaffeemaschine

**Version:** v2
**Datum:** 2026-09-11
**Auftraggeber:** Ralf Schmidt, Software Development Manager, SETEX Germany
**Bearbeiter:** Philipp Wagner, Praktikant OrgaTEX-Entwicklung
**Betreuung:** Gabriel
**Zeitbudget:** Stufe 1 ca. 10 Arbeitstage, Stufe 2 ca. 1 Arbeitstag

---

## Executive Summary

Die Kaffeemaschine in der OrgaTEX-Entwicklung muss jede Woche gereinigt und alle acht Wochen zusätzlich mit einem neuen Filter versehen werden. Bisher macht das, wer sich erbarmt. Du baust eine Windows-Anwendung, die für ein ganzes Jahr automatisch einen fairen Dienstplan erzeugt und ihn als Liste über die Kalenderwochen anzeigt.

Die Aufgabe wird zweimal gelöst: zuerst klassisch von Hand (Stufe 1), danach noch einmal von Grund auf mit Claude Code (Stufe 2). Der eigentliche Lerninhalt ist der Vergleich beider Wege.

---

## 1. Fachliche Anforderungen

### 1.1 Das Team

Sechs Personen aus der OrgaTEX-Entwicklung:

| Nr. | Name |
|---|---|
| 1 | Ralf |
| 2 | Jochen |
| 3 | Mario |
| 4 | Gabriel |
| 5 | Ehsan |
| 6 | Shariyar |

Die Liste muss im Programm änderbar sein — nicht fest in den Code geschrieben. Das Programm muss auch mit 4 oder 8 Personen funktionieren.

### 1.2 Die Aufgaben

| Aufgabe | Rhythmus | Beschreibung |
|---|---|---|
| Reinigung | jede Kalenderwoche | genau eine Person ist zuständig |
| Filtertausch | alle 8 Wochen | fällt zusätzlich in der jeweiligen Woche an |

**Wichtig:** Der Filtertausch wird von derselben Person erledigt, die in dieser Woche ohnehin reinigt. Es wird also *keine* zweite Person eingeteilt. Die Person hat in dieser Woche lediglich mehr Arbeit.

Der Filtertausch beginnt in KW 1 und wiederholt sich alle 8 Wochen: **KW 1, 9, 17, 25, 33, 41, 49** — das sind 7 Termine pro Jahr.

### 1.3 Der Zeitraum

Ein Kalenderjahr, eingeteilt in Kalenderwochen nach **ISO 8601** (die in Deutschland übliche Zählung). Achtung: Ein Jahr hat nicht immer 52 Wochen. 2026 und 2032 haben zum Beispiel 53 Wochen. Das Programm muss das korrekt behandeln.

### 1.4 Was heißt „gleichverteilt"?

Das ist die wichtigste Frage der ganzen Aufgabe, und sie ist bewusst noch nicht beantwortet. Es gibt nämlich zwei verschiedene Dinge, die man verteilen kann:

1. die **Anzahl der Reinigungen** pro Person
2. die **Anzahl der Filtertausche** pro Person

Ein Plan kann bei (1) perfekt sein und bei (2) katastrophal. Das gilt es herauszufinden.

**Verbindliche Festlegung für diese Aufgabe — beides muss gelten:**

| Kriterium | Bedingung |
|---|---|
| F1 | Differenz zwischen der Person mit den meisten und der mit den wenigsten **Reinigungen** ≤ 1 |
| F2 | Differenz zwischen der Person mit den meisten und der mit den wenigsten **Filtertauschen** ≤ 1 |
| F3 | Keine Person ist in zwei aufeinanderfolgenden Kalenderwochen dran |

Bei 6 Personen und 52 Wochen bedeutet das: vier Personen reinigen 9× und zwei Personen 8× (4 × 9 + 2 × 8 = 52), und beim Filter hat eine Person 2 Termine und fünf Personen je 1 Termin (1 × 2 + 5 × 1 = 7).

---

## 2. Akzeptanzkriterien

Die Aufgabe gilt als erledigt, wenn alle folgenden Punkte erfüllt und nachweisbar sind. „Nachweisbar" heißt: es gibt einen automatisierten Test dafür oder du kannst es in der laufenden Anwendung vorführen.

| Nr. | Kriterium | Nachweis |
|---|---|---|
| A1 | Das Programm erzeugt für ein wählbares Jahr einen vollständigen Plan über alle Kalenderwochen | Vorführung |
| A2 | Jede Kalenderwoche hat genau eine zuständige Person | Unit-Test |
| A3 | Die Filter-Wochen 1, 9, 17, 25, 33, 41, 49 sind als solche markiert | Unit-Test |
| A4 | Kriterium F1 ist erfüllt (Reinigungen ausgeglichen) | Unit-Test |
| A5 | Kriterium F2 ist erfüllt (Filtertausche ausgeglichen) | Unit-Test |
| A6 | Kriterium F3 ist erfüllt (keine zwei Wochen hintereinander) | Unit-Test |
| A7 | Jahre mit 53 Kalenderwochen werden korrekt behandelt (Test mit 2026) | Unit-Test |
| A8 | Die Mitarbeiterliste ist in der Oberfläche pflegbar (hinzufügen, entfernen) | Vorführung |
| A9 | Mitarbeiterliste und Plan werden als JSON-Datei gespeichert und wieder geladen | Vorführung |
| A10 | Der Plan lässt sich als CSV exportieren und in Excel öffnen | Vorführung |
| A11 | Das Programm stürzt bei fehlerhaften Eingaben nicht ab (leere Liste, 1 Person, ungültiges Jahr) | Vorführung |
| A12 | Es gibt eine Statistik-Ansicht: Reinigungen und Filtertausche pro Person | Vorführung |

---

## 3. Technische Vorgaben

Diese Vorgaben gelten für **beide** Stufen. Das ist der Kern des Experiments: gleiche Aufgabe, gleiche Bausteine, unterschiedlicher Arbeitsweg.

| Punkt | Vorgabe |
|---|---|
| Sprache | C# |
| Zielframework | .NET 10 (LTS) |
| Oberfläche | WPF (Windows Presentation Foundation) |
| Entwicklungsumgebung | Visual Studio 2022 oder Visual Studio 2026 |
| Tests | MSTest |
| Datenhaltung | JSON-Datei über `System.Text.Json` |
| Fremdbibliotheken | **keine** — nur was im .NET SDK enthalten ist |

> **Hinweis zur .NET-Version:** .NET 8 und .NET 9 erreichen am 10. November 2026 ihr Support-Ende. .NET 10 ist seit dem 11. November 2025 verfügbar und wird als LTS-Release bis November 2028 unterstützt. Deshalb wird hier .NET 10 verwendet. Quelle: [.NET 8 and .NET 9 will reach End of Support on November 10, 2026](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/)

### Warum keine Fremdbibliotheken?

Weil du verstehen sollst, was passiert. Für fast jedes Problem in dieser Aufgabe gibt es ein fertiges NuGet-Paket. Wenn du es benutzt, lernst du das Paket. Wenn du es selbst baust, lernst du das Problem. In Stufe 1 ist das Ziel, das Problem zu lernen.
