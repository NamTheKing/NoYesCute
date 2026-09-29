# NoYesCute 💍

A small WPF app that asks **"Will you marry me?"** with a **YES** and a **NO** button.
The NO button runs away to a random spot whenever the mouse comes near, so the
only button you can actually click is YES.

## Run

Requires Windows and the .NET 8 SDK.

```bash
cd NoYesCute
dotnet run
```

Or open `NoYesCute/NoYesCute.csproj` in Visual Studio and press F5.

## Animations used

| What | How |
|------|-----|
| NO button escapes | `DoubleAnimation` on `Canvas.Left` / `Canvas.Top` with `CubicEase`, 150 ms getting faster down to 60 ms |
| YES grows every time NO escapes | `DoubleAnimation` on a `ScaleTransform` with `BackEase` |
| Celebration fades in | `DoubleAnimation` on `Opacity` |
| Heart beats | `DoubleAnimation` with `AutoReverse` + `RepeatBehavior.Forever` |

## How NO stays unclickable

- `MouseMove` on the canvas → move away as soon as the mouse is within 70 px (before it even touches NO).
- `MouseEnter` → move away (backup).
- `PreviewMouseLeftButtonDown` / `PreviewTouchDown` → `e.Handled = true`, so even a
  lucky press never raises `Click`.
- `Focusable="False"` and `IsTabStop="False"` → you can't reach it with Tab + Enter.
- The new spot is chosen at least 150 px from the mouse and never on top of YES.
