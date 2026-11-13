# LayoutFlag

[![GitHub Release](https://img.shields.io/github/v/release/thejohnd0e/LayoutFlag?include_prereleases&color=blue&label=Release)](https://github.com/thejohnd0e/LayoutFlag/releases/latest)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(64--bit)-0078D6?logo=windows&logoColor=white)](https://github.com/thejohnd0e/LayoutFlag)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

[English](#english) | [Русский](#русский)

---

## Русский

**LayoutFlag** — компактный плавающий индикатор текущей раскладки клавиатуры (RU / EN) для Windows.

### Для чего создано

На современных рабочих местах с мониторами высокого разрешения (2K, 4K) и многомониторных конфигурациях стандартный значок языка в трее Windows становится практически незаметным:
- приходится постоянно отвлекаться и «шарить глазами» по экрану в угол;
- на широком рабочем столе или нескольких дисплеях трей находится слишком далеко от зоны набора текста;
- легко ошибиться раскладкой и напечатать текст не теми символами.

**LayoutFlag** решает эту проблему: прямо на экране поверх всех окон размещается небольшая аккуратная плашка с текущей раскладкой, которую можно перетащить в любое удобное для глаз место — например, рядом с активной рабочей зоной или на каждый используемый монитор.

### Возможности

- **Всегда под рукой:** плашка отображается поверх всех окон и не перехватывает на себя фокус ввода при клике или перетаскивании.
- **Поддержка нескольких мониторов:** можно включить от 1 до 4 синхронных плашек (например, разместить по одной на каждом экране).
- **Свободное перемещение:** каждую плашку можно перетащить мышью в любую точку экрана, позиции автоматически запоминаются.
- **Настройка внешнего вида через контекстное меню:**
  - **Вид:** флаги стран или минималистичные цветные плашки (синий / красный).
  - **Форма:** скруглённая, прямоугольная или круг.
  - **Размер:** маленький, средний или крупный.
- **Индикатор в системном трее:** иконка в трее также динамически меняется в зависимости от активной раскладки.
- **Двуязычный интерфейс:** меню на русском и английском языках (с автоопределением по языку системы).
- **Автозапуск:** удобное включение автозагрузки вместе с Windows в один клик.

### Системные требования

- **Операционная система:** Windows 10 или Windows 11 (64-бит).
- **Мониторы:** один или несколько дисплеев любого разрешения и масштаба (полная поддержка Per-Monitor V2 DPI).
- **Инсталлятор:** автономный (standalone / self-contained) — не требует предварительной установки .NET Runtime.

---

## English

**LayoutFlag** is a lightweight, floating keyboard layout indicator (RU / EN) for Windows.

### Why It Was Created

On modern high-resolution displays (2K, 4K) and multi-monitor setups, the default Windows tray language icon is often hard to notice:
- You frequently have to look away and scan across screens just to check the layout;
- On wide desks and multi-display setups, the system tray is far from your actual typing area;
- It's easy to start typing in the wrong language and have to retype text.

**LayoutFlag** solves this issue by placing a neat, compact floating badge with the current layout on top of all windows. You can drag it anywhere close to your line of sight — next to your active work area or on each monitor you use.

### Features

- **Always in sight:** Badges stay on top of all windows and never steal input focus when clicked or dragged.
- **Multi-monitor support:** Display anywhere from 1 to 4 synchronized badges (e.g., one on each screen).
- **Freely movable:** Drag any badge to your preferred location; positions are saved automatically.
- **Customizable appearance via context menu:**
  - **Style:** Country flags or clean solid colors (blue / red).
  - **Shape:** Rounded rectangle, sharp rectangle, or circle.
  - **Size:** Small, medium, or large.
- **Dynamic tray icon:** The system tray icon also reflects the active keyboard layout.
- **Bilingual interface:** Russian and English UI with automatic system language detection.
- **Startup option:** Easily toggle launch on Windows startup with a single click.

### System Requirements

- **Operating System:** Windows 10 or Windows 11 (64-bit).
- **Monitors:** Single or multiple displays of any resolution and scale (full Per-Monitor V2 DPI support).
- **Installer:** Standalone (self-contained) — does not require a pre-installed .NET Runtime.
