<h1 align="center">HowLongToBeat for PowerToys Run</h1>

<p align="center">
Search HowLongToBeat game completion times directly from PowerToys Run.
</p>

An unofficial PowerToys Run plugin that lets you search games, compare Main Story, Main + Extras, and Completionist times, filter ambiguous results, and open the matching HowLongToBeat page without leaving the launcher.

## Contents

1. [Demo](#demo)
2. [Features](#features)
3. [Architecture](#architecture)
4. [Search Modifiers](#search-modifiers)
5. [How to Install](#how-to-install)
6. [Building from Source](#building-from-source)
7. [License and Third-Party Notices](#license-and-third-party-notices)
8. [Acknowledgements](#acknowledgements)

## Demo



https://github.com/user-attachments/assets/485383d6-b540-4aa5-9506-6a691dd6c156



## Features

- Search HowLongToBeat directly from PowerToys Run using the `hltb` action keyword.
- View Main Story, Main + Extras, and Completionist times directly in the result list.
- Search games by title or directly by HowLongToBeat game ID.
- Disambiguate remakes and similarly named games using release year and platform preferences.
- Search only DLCs or exclude DLC/mod results.
- Local result ranking combines exact-title matches, aliases, similarity, year, platform, and game type.
- In-memory caching reduces repeated HowLongToBeat requests and makes repeated searches nearly instant.
- Configurable typing delay reduces unnecessary requests while a query is still being entered.
- A persistent helper process avoids repeatedly starting Python for consecutive searches.
- Configurable idle shutdown automatically stops the helper process when it is no longer needed.
- Automatic bridge recovery retries once if the helper process unexpectedly exits during a request.
- Stale queries are cancelled when a newer query replaces them.
- Result context actions can open HowLongToBeat, copy completion times, or copy the HowLongToBeat link.
- Browser fallback is available when a search cannot be completed through the helper.
- Python is bundled with release builds; users do not need a separate Python installation.

## Architecture

The plugin uses a C# PowerToys Run frontend together with a packaged Python helper process.

- **PowerToys Run plugin – C#**
  - Integrates with the PowerToys Run plugin API.
  - Parses search modifiers and validates user input.
  - Handles query cancellation and configurable search delay.
  - Performs local result ranking and formatting.
  - Maintains the in-memory search cache.
  - Provides result actions and plugin settings.
  - Manages the lifecycle of the Python helper.

- **HowLongToBeat bridge – Python**
  - Runs as a persistent packaged helper process.
  - Communicates with the C# plugin using newline-delimited JSON over standard input/output.
  - Uses `howlongtobeatpy` to perform HowLongToBeat searches.
  - Supports title searches, DLC modes, direct game-ID lookups, cancellation, ping, and graceful shutdown.
  - Is bundled using PyInstaller, so end users do not need Python installed.

- **Search result processing**
  - HowLongToBeat results are returned to C# with their original timing values and metadata.
  - C# applies local ranking using title quality, aliases, similarity, release year, platform preference, and result type.
  - Year and platform modifiers influence ranking locally rather than unnecessarily removing potentially useful results.
  - Search responses are cached in memory to reduce repeated network requests.
 
<details>
<summary>Architecture Image</summary>

<img width="512" alt="HLTB plugin Arch" src="https://github.com/user-attachments/assets/2793641c-ad52-4339-846f-e79515f84d05" />

</details>

#### Configurable Options

The following options are available from the HowLongToBeat extension settings inside PowerToys Run:

| Setting                   |    Default | Description                                                                                                                         |
| ------------------------- | ---------: | ----------------------------------------------------------------------------------------------------------------------------------- |
| Bridge idle shutdown      | 10 minutes | Stops the packaged helper after the configured period without bridge activity. Set to `0` to keep it running until PowerToys exits. |
| Search delay while typing |     400 ms | Waits after the latest query change before contacting HowLongToBeat. Set to `0` to disable the additional delay.                    |

The helper is started lazily. Simply launching PowerToys does not start the Python bridge.

## Search Modifiers

Start every query with the `hltb` action keyword.

| Modifier                | Example                              | Behaviour                                                 |
| ----------------------- | ------------------------------------ | --------------------------------------------------------- |
| None                    | `hltb Elden Ring`                    | Performs a normal title search.                           |
| `--year <year>`         | `hltb Resident Evil 4 --year 2005`   | Strongly prefers results from the requested release year. |
| `--platform <platform>` | `hltb Resident Evil 4 --platform gc` | Prefers games available on the requested platform.        |
| `--dlc`                 | `hltb Elden Ring --dlc`              | Searches DLC results only.                                |
| `--no-dlc`              | `hltb Elden Ring --no-dlc`           | Hides DLC/mod results.                                    |
| `id:<game-id>`          | `hltb id:68151`                      | Looks up a specific HowLongToBeat game ID directly.       |

Common platform aliases such as `pc`, `ps4`, `ps5`, `xb1`, `xsx`, `switch`, `switch2`, and `gc` are supported.

Modifiers can also be combined where applicable:

```text
hltb Resident Evil 4 --year 2005 --platform gc
```

Direct ID lookups cannot be combined with title-search modifiers.

Both `--year` and `--platform` also support `=` between the modifier and its value, for example `--year=2023` or `--platform=ps5`.

## How to Install

Prebuilt releases currently target **Windows x64**.

1. Grab the latest release ZIP from GitHub Releases.
2. Close PowerToys completely.
3. Extract the ZIP so the plugin folder is located at:

```text
%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\HowLongToBeat
```

The resulting structure should resemble:

```text
HowLongToBeat\
├── Community.PowerToys.Run.Plugin.HowLongToBeat.dll
├── Community.PowerToys.Run.Plugin.HowLongToBeat.deps.json
├── plugin.json
├── Images\
└── Bridge\
    ├── hltb-bridge.exe
    └── _internal\
```

4. Start PowerToys again.
5. Open **PowerToys Settings → PowerToys Run → Plugins** and confirm that **HowLongToBeat** is enabled.
6. Open PowerToys Run and try:

```text
hltb Elden Ring
```

When upgrading, close PowerToys, remove the previous `HowLongToBeat` plugin folder, extract the new release, and start PowerToys again.

## Building from Source

> **Architecture support:** The current release and build scripts are tested for Windows x64 only. ARM64 support is planned but is not yet implemented or validated end to end.

The project is developed and tested on Windows and currently targets the PowerToys version specified in `POWERTOYS_VERSION`.

> **Building against another PowerToys version:** Update `POWERTOYS_VERSION`, run `scripts\sync-powertoys-deps.cmd` again, and rebuild and test the project. Compatibility with other PowerToys versions is not guaranteed until verified.

### Prerequisites

- Windows
- PowerToys
- .NET SDK 10
- Python 3.11+
- Git

Clone the repository and enter the project directory:

```bat
git clone https://github.com/SharnavM/PowerToysRun-HowLongToBeat
cd PowerToysRun-HowLongToBeat
```

Create the Python virtual environment:

```bat
python -m venv .venv
.venv\Scripts\activate
```

Install the Python dependencies:

```bat
python -m pip install --upgrade pip
pip install -r src\bridge\requirements-lock.txt
```

Copy the required PowerToys runtime references into the local dependency directory:

```bat
scripts\sync-powertoys-deps.cmd
```

> If PowerToys is installed in a non-standard directory, set `POWERTOYS_INSTALL_DIR` before running the dependency-sync or deployment scripts.

Run the Python tests:

```bat
python -m pytest
```

Build the packaged Python bridge:

```bat
scripts\build-bridge.cmd
```

Run the C# plugin tests:

```bat
scripts\test-plugin.cmd
```

Build the plugin:

```bat
scripts\build-plugin.cmd
```

The combined plugin output is written to:

```text
artifacts\plugin\HowLongToBeat
```

For a production release build and release archive:

```bat
scripts\build-release.cmd
```

Release archives are written under:

```text
artifacts\release
```

## License and Third-Party Notices

This project is distributed under the terms described in the repository's `LICENSE` file.

The release package also contains or makes use of third-party software. Relevant notices and license texts are maintained under `third_party/licenses`.

Important third-party components include:

- **howlongtobeatpy** – used for communicating with HowLongToBeat; distributed under the MIT License.
- **Python** – the packaged bridge includes the Python runtime; Python is distributed under the Python Software Foundation License.
- **PyInstaller bootloader** – used to package the Python helper; distributed under GPL-2.0-or-later with the PyInstaller bootloader exception.

PowerToys runtime assemblies used by the plugin are supplied by the user's PowerToys installation and are not bundled in the release package.

This is an unofficial community project. It is not affiliated with, endorsed by, or officially supported by Microsoft, PowerToys, or HowLongToBeat.

## Acknowledgements

1. [**ScrappyCocco**](https://github.com/ScrappyCocco) - for creating and maintaining the [`howlongtobeatpy`](https://github.com/ScrappyCocco/HowLongToBeat-PythonAPI) library that provides the HowLongToBeat integration used by this project.

2. **AI-assisted engineering** - this plugin was engineered in conjunction with AI. I started without the depth of C# and PowerToys Run plugin experience required to build it independently, so AI was used as a development partner while I reviewed, tested, debugged, and validated the implementation throughout each milestone.
