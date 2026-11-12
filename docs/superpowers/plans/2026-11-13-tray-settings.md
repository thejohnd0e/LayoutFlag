# Tray Settings Implementation Plan

**Goal:** Подменю настроек (вид, форма, размер) в трее и меню плашки с динамической отрисовкой и сохранением.

**Spec:** `docs/superpowers/specs/2026-11-13-tray-settings-design.md`

### Task 1: Enums и расширение AppSettings
- Добавить `BadgeMode`, `BadgeShape`, `BadgeSize` и поля в `AppSettings` со значениями по умолчанию (`Flags`, `Rounded`, `Medium`).
- Тесты (TDD): старый JSON без новых полей загружается с дефолтами; round-trip новых полей; невалидный enum → дефолты.

### Task 2: BadgeGeometry и BadgeRenderer
- `BadgeGeometry.GetPixelSize(BadgeShape, BadgeSize)` и `CreateShapePath(BadgeShape, int w, int h)`.
- Тесты (TDD): размеры для всех комбинаций Shape × Size; валидный непустой `GraphicsPath` для всех форм.
- `BadgeRenderer.Render(FlagId, BadgeMode, BadgeShape, int w, int h)` — рисует флаг (RU/US/Unknown) или одноцветную плашку в пределах формы.

### Task 3: Интеграция в FlagForm и публикация
- Заменить статичный `PictureBox` + PNG на прямую буферизованную отрисовку (`DoubleBuffered = true`, кэш `Bitmap`, обновление `Region` и `Size` на лету).
- Единый `ContextMenuStrip` для трея и плашки с подменю `Вид`, `Форма`, `Размер` и синхронизацией галочек в `Opening`.
- Пофиксить клэмп в `OnLocationChanged` под маленькие размеры (`Math.Min(Width, 20)` и `Screen.Bounds`).
- Прогнать `dotnet test`, опубликовать `publish\LayoutFlag.exe`, перезапустить и закоммитить.
