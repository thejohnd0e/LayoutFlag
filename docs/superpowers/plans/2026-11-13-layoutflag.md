# LayoutFlag Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Один exe, показывающий поверх всех окон перетаскиваемую плашку с флагом текущей раскладки (RU/EN).

**Architecture:** WinForms-окно без рамки, topmost, WS_EX_NOACTIVATE; таймер 100 мс читает раскладку активного окна через WinAPI и подменяет картинку флага. Позиция — %APPDATA%\LayoutFlag\settings.json. Автозапуск — ярлык в shell:startup.

**Tech Stack:** C#, .NET 8, WinForms, P/Invoke (user32.dll), System.Text.Json.

**Spec:** `docs/superpowers/specs/2026-11-13-layoutflag-design.md`

## Global Constraints

- Windows 10/11; публиковать `dotnet publish -c Release` как framework-dependent single-file exe `LayoutFlag.exe`.
- Флаги — PNG-ресурсы, встроенные в exe (`flag_ru`, `flag_us`, `flag_unknown`).
- Неизвестный язык → серая плашка `flag_unknown`.
- Поведение: клики по плашке не активируют её (WS_EX_NOACTIVATE).

## Review Focus

- Раскладка читается у **окна с фокусом**, а не глобально — у разных окон разные раскладки.
- Нет активного окна / рабочий стол → показать последний известный флаг, не падать.
- Drag не должен ронять плашку за пределы экрана (клэмпить в границы виртуального экрана).
- Повторный запуск приложения (второй exe) не должен создавать вторую плашку — single-instance (Mutex).
- Битый/отсутствующий settings.json → позиция по умолчанию (правый нижний угол), не падать.

---

### Task 1: Проект и определение языка раскладки

**Files:**
- Create: `LayoutFlag/LayoutFlag.csproj`
- Create: `LayoutFlag/Program.cs`
- Create: `LayoutFlag/LayoutDetector.cs`
- Test: `LayoutFlag.Tests/LayoutFlag.Tests.csproj`, `LayoutFlag.Tests/LayoutDetectorTests.cs`

**Interfaces:**
- Produces: `static class LayoutDetector { public static FlagId GetFlagForHkl(IntPtr hkl) }`, `enum FlagId { Ru, En, Unknown }`. `Program.Main` — single-instance Mutex + запуск формы.

- [ ] **Step 1: Создать решения/проекты**

`dotnet new winforms -n LayoutFlag`, `dotnet new xunit -n LayoutFlag.Tests`, ссылка test→main.

- [ ] **Step 2: Тест определения языка из HKL**

LANGID — младшие 16 бит HKL: 0x0419 → `FlagId.Ru`, 0x0409 → `FlagId.En`, 0x0407 (de) → `FlagId.Unknown`.

```csharp
[Theory]
[InlineData(0x04190419u, FlagId.Ru)]
[InlineData(0x04090409u, FlagId.En)]
[InlineData(0x04070407u, FlagId.Unknown)]
public void GetFlagForHkl_MapsLangId(IntPtr hkl, FlagId expected)
    => Assert.Equal(expected, LayoutDetector.GetFlagForHkl(hkl));
```

- [ ] **Step 3: Реализовать `LayoutDetector`** — `(ushort)(hkl.ToInt64() & 0xFFFF)` → switch.

- [ ] **Step 4: `dotnet test`** — PASS.

- [ ] **Step 5: Program.cs: Mutex single-instance** (`Local\LayoutFlagSingleInstance`), при повторе — выход код 0. Проверить: два запуска → один процесс.

### Task 2: Окно-плашка с флагами и drag

**Files:**
- Create: `LayoutFlag/FlagForm.cs`
- Create: `LayoutFlag/Flags/russia.png`, `usa.png`, `unknown.png` (ресурсы)
- Create: `LayoutFlag/NativeMethods.cs`

**Interfaces:**
- Consumes: `LayoutDetector.GetFlagForHkl`, `FlagId`.
- Produces: `class FlagForm : Form` с таймером опроса; `NativeMethods.GetForegroundWindow()`, `GetWindowThreadProcessId`, `GetKeyboardLayout(IntPtr)`.

- [ ] **Step 1: PNG-ресурсы** — сгенерировать/положить три PNG (~40×28 рендер), `EmbeddedResource` в csproj.

- [ ] **Step 2: FlagForm** — borderless, `TopMost=true`, `ShowInTaskbar=false`, размеры 48×34,背景 плашки чуть скруглённые (просто BackColor), картинка `PictureBox` dock. `CreateParams` добавляет `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` (0x8000000 | 0x80).

- [ ] **Step 3: Таймер 100 мс** — `GetForegroundWindow` → `GetWindowThreadProcessId` → `GetKeyboardLayout(tid)` → `LayoutDetector` → смена изображения при изменении. Если foreground == IntPtr.Zero — оставить прежний флаг.

- [ ] **Step 4: Drag** — `WM_NCLBUTTONDOWN`/`HTCAPTION` через `ReleaseCapture + SendMessage` на MouseDown левой кнопки. Клэмп позиции в `Screen.FromControl(this).Bounds` после перемещения (LocationChanged).

- [ ] **Step 5: Ручная проверка** — `dotnet run`: плашка видна, переключение Alt+Shift меняет флаг, drag работает поверх полноэкранных окон.

### Task 3: Позиция, автозапуск, публикация

**Files:**
- Create: `LayoutFlag/AppSettings.cs` (load/save `%APPDATA%\LayoutFlag\settings.json`, try/catch → default)
- Modify: `LayoutFlag/FlagForm.cs` (загрузка/сохранение позиции, контекстное меню)

**Interfaces:**
- Produces: `AppSettings { Point Location; bool AutoStart; }`, `static AppSettings Load()`, `void Save()`; `Shortcut.ToggleAutostart(bool)` — ярлык `LayoutFlag.lnk` в `shell:startup` через WScript.Shell COM.

- [ ] **Step 1: AppSettings.Load/Save** — System.Text.Json, try/catch на всё; default — правый нижний угол (`Screen.PrimaryScreen.WorkingArea` минус размер плашки и отступ 16px).
- [ ] **Step 2: Тест**: битый JSON-файл → Load не бросает, даёт default (тест пишет во временную папку, путь параметризовать `Load(string path)`).
- [ ] **Step 3: Контекстное меню** — правый клик: галочка «Автозапуск» (ToggleAutostart), «Выход» (Close). Сохранение позиции в `FormClosing`/`LocationChanged`.
- [ ] **Step 4: ToggleAutostart** — COM `WScript.Shell`, создание/удаление `.lnk` в `shell:startup` (`Environment.GetFolderPath(SpecialFolder.Startup)`), target = `Environment.ProcessPath`.
- [ ] **Step 5: Публикация** — `dotnet publish -c Release -p:PublishSingleFile=true --self-contained false` → `publish/LayoutFlag.exe`. Запустить, проверить exe напрямую.
- [ ] **Step 6: Ручной смоук** — перезапуск помнит позицию; автозапуск кладёт ярлык; второй запуск не даёт вторую плашку.
