# BREATHE

Lab instrument control and data analysis system for transepithelial electrical current (TECC) measurements on cell culture plates.

## Repository layout

| Folder / File | Contents |
|---|---|
| `breathe-z-src/` | Instrument control application (WPF, C#, .NET Framework 4.8) |
| `breathe-calc-src/` | Data analysis application (WPF, C#, .NET Framework 4.8) |
| `breathe-z-docs/` | Hardware docs, wiring diagrams, quickstart guide, protocol descriptions |
| `compiled-binaries/` | Pre-built zips of both applications |
| `existing-instruments/` | Per-instrument folders (Z-1, Z-2, Z-6) with SolidWorks solid models and configuration files (deck positions, etc.) |
| `BREATHE Software Info.docx` | Software information document |

## BREATHE-Z (instrument control)

WPF application that drives a BREATHE-Z instrument — TECC measurement boards, Newport ESP-300 motion controller, and NI DAQ hardware.

Solution: `breathe-z-src/BreatheZ.sln`

Projects:

- **BreatheZ.Ui** — Main WPF application
- **Cf.Tecc.Controller** — TECC board communication
- **Cf.Daq.Common** — NI DAQ integration
- **Cf.TeccViewer** — TECC data viewer

## BREATHE Data Cruncher (data analysis)

WPF application that loads TECC measurement files, calculates resistance/voltage/current equivalents and derived metrics (AUC, delta values across protocol stages), and exports results.

Solution: `breathe-calc-src/BreatheCalc.FourDispense.sln`

Projects:

- **BreatheCalc.FourDispense** — Main WPF application
- **BreatheCalc.DataAccess** — Data loading and persistence

## Prerequisites and building

- .NET Framework 4.8
- Visual Studio 2022

Open either solution and build. NuGet packages restore automatically. Internal dependency DLLs are checked into the `Dependencies/` folders within each source tree, so no additional package feeds are needed.

**BREATHE-Z runtime requirement:** The instrument control app requires National Instruments DAQmx drivers to be installed on the machine that will communicate with NI hardware.

## Pre-built binaries

Ready-to-run zips are in `compiled-binaries/`:

- `breathe-z.zip` — BREATHE-Z instrument control
- `breathe-calc.zip` — BREATHE Data Cruncher

## Documentation

Hardware and protocol documentation is in `breathe-z-docs/`:

- **Breathe quickstart guide.docx** — Getting-started guide
- **High Level System Description.docx** — System overview
- **High Level Protocol Phases.docx** — TECC measurement protocol stages
- **BREATHE-Z Electronics.docx** — Electronics documentation
- **DAQ Controller Command Overview.docx** — DAQ command reference
- **High Level Electrical.vsdx** — Electrical diagram
- **Breathe Z Diagram.vsd** — System diagram
- **Workflow Cartoon.xlsx** — Workflow illustration
