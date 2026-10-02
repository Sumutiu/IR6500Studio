# IR6500 Studio

Windows control software for the **ACHI IR6500** BGA/IR rework station with the **ALTEC PC410** temperature controller.

© 2026 Marius Sumutiu. All rights reserved.

## What it does

- **Dashboard**: live temperature (PV), setpoint (SP) and heater output, plotted on a chart with the planned profile drawn over it. Start, hold, resume and stop the program on the controller. Every run is recorded to CSV automatically.
- **Profiles**: an 8-step editor (ramp °C/s → target °C → hold s) with a live preview, total time and peak temperature. Profiles are saved as `.ir6profile` files, and you can read or write any of the controller's 10 patterns (0–9) with read-back verification.
- **Console**: the raw serial traffic (TX/RX, decoded and in hex), plus manual read or write of any parameter for troubleshooting.
- **Settings**: COM port, baud rate, address, timeouts, DTR/RTS, temperature limit, logging folder, and a **built-in simulator** so you can try everything without the station.

## Requirements

- Windows 10 or 11 (.NET Framework 4.8 is already installed on both).
- A serial connection to the PC410. This is usually a USB-to-RS232 adapter on the station's communication port.

## Running

`IR6500Studio.exe` is a single file with no installer and no extra DLLs. Copy it anywhere and run it.
The `IR6500Studio.exe.config` that the build produces is optional.

Windows may show a SmartScreen prompt the first time, because the exe is not code-signed (*More info → Run anyway*).

## Building with Visual Studio 2026

1. Open `IR6500Studio.slnx`.
2. Select **Release** and build (*Build → Build Solution*).
3. The exe is written to `src\IR6500Studio\bin\Release\IR6500Studio.exe`.

The project is SDK-style and targets `net48`. If the *.NET Framework 4.8 targeting pack* component is not installed, the build restores the reference assemblies from NuGet automatically.

`tests\IR6500Studio.Tests` is a small console test runner with no dependencies (set it as the startup project and press F5). It checks the protocol framing and checksums against the documented examples, and does a full profile upload/download and a program run against the simulator.

### Project layout

```
src/IR6500Studio/
  Protocol/   Pc410Protocol (frames, BCC), Pc410Client (transactions, retries),
              Pc410Device (telemetry, profiles, run/hold/stop), SerialTransport, SimulatedPc410
  Model/      ReflowProfile (+ file format, presets, planned curve), AppSettings, CsvRecorder
  Services/   DeviceService (connection + background polling)
  UI/         Theme, custom dark controls, chart, pages, main window
tests/IR6500Studio.Tests/
```

### Code style

`.editorconfig` holds the formatting rules (opening braces on the same line, 4-space indent, CRLF). Visual Studio applies them when you format a document (Ctrl+K, Ctrl+D) or on Code Cleanup, and `dotnet format whitespace` uses them too.

## Protocol notes (PC410 / AL808)

- **Serial format:** 7 data bits, even parity, 1 stop bit (**7E1**), 300–19200 baud.
- **Read:** `EOT A A B B C1 C2 ENQ` → `STX C1 C2 DATA ETX BCC`. Each address digit is sent twice, so address 01 is sent as `0011`.
- **Write:** `EOT A A B B STX C1 C2 DATA ETX BCC` → `ACK` or `NAK`.
- **BCC** is the XOR of every byte after STX, including ETX.
- **Parameters used:** `PV` `SP` `OP` (telemetry), `OS` (program: `>0000` stop, `>0002` run, `>0003` hold), `SW` (status), `SE` (segment), `ch` (pattern 0–9), `Hb` (holdback), and `r1–r8` / `l1–l8` / `t1–t8` (ramp °C/s, target °C, hold s).
- **Unused steps:** by default they are written as `0/0/0`. You can choose to write `END` on the first unused ramp instead (in Settings).

## Troubleshooting

| Symptom | Check |
|---|---|
| "The controller did not answer" | COM port, baud rate (must match the controller), address, cable. The format must be 7E1. |
| "Port is in use" | Close PSoft or any terminal program that has the COM port open. |
| Works with PSoft but not here | Compare its baud rate and address. Try toggling DTR/RTS (some isolated adapters take power from them). |
| Values read back slightly different after writing | The controller rounds some values. The warning dialog lists each difference. |

## Files and folders

- Settings: `%APPDATA%\IR6500 Studio\settings.xml`
- Profiles (default): `Documents\IR6500 Studio\Profiles`
- CSV logs (default): `Documents\IR6500 Studio\Logs`

## Safety

The station heats to soldering temperatures. The controller keeps running a program even if this software closes or loses the connection. Never leave a run unattended. Verify profiles with an external thermocouple before using them on valuable boards.
