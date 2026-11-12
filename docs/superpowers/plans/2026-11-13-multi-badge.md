# Multi-Badge Implementation Plan

**Goal:** 1–3 синхронные плашки в одном процессе, умное размещение по мониторам, сохранение позиций.

**Spec:** `docs/superpowers/specs/2026-11-13-multi-badge-design.md`

### Task 1: AppSettings (`BadgeCount`, `Locations`, миграция `Location`) + `BadgePlacement`
- Добавить `BadgeCount` (1..3) и `Locations` (`List<Point>`) в `AppSettings`, миграцию legacy `Location` при `Load`.
- Добавить `BadgePlacement.GetDefaultLocation(...)` с выбором свободного монитора / смещением.
- Тесты (TDD) в `AppSettingsTests.cs` и `BadgePlacementTests.cs`.

### Task 2: `BadgeWindow` + `TrayAppContext` + `Program.cs`
- Выделить `BadgeWindow : Form` (per-pixel alpha `UpdateLayeredWindow`, drag, `OnDpiChanged`, клэмп).
- Создать `TrayAppContext : ApplicationContext` (таймер, трей-иконки, общее меню с `Плашек -> 1/2/3`, синхронизация всех окон, сохранение `Locations`).
- Обновить `Program.cs` на `new TrayAppContext()`, удалить `FlagForm.cs`.
- Прогнать `dotnet test`, опубликовать `publish\LayoutFlag.exe`, перезапустить, проверить.
