# SDD ledger — plan: docs/superpowers/plans/2026-11-13-layoutflag.md

Pre-flight: Task 2 consumes LayoutDetector.GetFlagForHkl/FlagId (produces окно-плашка); Task 3 consumes FlagForm для позиции/меню. Конфликтов нет; имена из плана зафиксированы.

Task 1: complete (commits 769b3dc..c7a56a0, tests: dotnet test LayoutFlag.Tests → 4/4 PASS). Ruling: Program.cs (Mutex) закоммичен вместе с Task 2, т.к. ссылается на FlagForm — иначе небилдящийся коммит.
Task 2: complete (commit 55dbd8b, build 0 errors, smoke: процесс живёт, тесты 4/4).
Task 3: complete (commit 2fbfb18, tests 7/7, publish single-file 164 КБ, второй запуск exited=true).
Final review: свежий субагент-ревьюер (ses_e2e205157ffeK05tkgmo27I8Kc). Вердикт: With fixes. Important #1 (Shortcut без try/catch) — fixed в 05dc518, тесты 6/6, смоук-запуск жив. Ruling: COM-путь WScript.Shell не юнит-тестируется без тяжёлых моков — фикс проверен build+smoke, не RED→GREEN.
Final: fixed minor 2 (утечка ContextMenuStrip) и 4 (мёртвое поле AutoStart) и 6 (*.user/UnitTest1 мусор) в том же коммите — дёшево, рядом с важным фиксом.
Final: minor (deferred): DPI PerMonitorV2 для многомониторных конфигураций с разным масштабом.
Final: minor (deferred): debounce-сохранение позиции при LocationChanged (сейчас только FormClosing).
Final: minor (deferred): README с publish-командой и заметкой, что ярлык фиксирует путь к exe.
