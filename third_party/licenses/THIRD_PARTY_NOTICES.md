# Third-Party Notices

This directory contains license texts and notices for third-party software used by, bundled into, or involved in producing the packaged HowLongToBeat bridge.

The current bridge pins `howlongtobeatpy==1.0.23`. That release declares these direct dependencies: `aiohttp~=3.14`, `requests~=2.34`, `aiounittest~=1.5`, `fake_useragent~=2.2`, and `beautifulsoup4~=4.15`. The packaged bridge also includes Python and is produced with PyInstaller. License files for common transitive dependencies bundled by those packages are included here as well.

Included notices cover:

- howlongtobeatpy
- Python / CPython
- PyInstaller
- PyInstaller community hooks
- aiohttp
- aiohappyeyeballs
- aiosignal
- attrs
- frozenlist
- multidict
- propcache (including NOTICE)
- yarl
- requests
- certifi
- charset-normalizer
- idna
- urllib3
- aiounittest
- fake-useragent
- beautifulsoup4
- soupsieve
- typing-extensions
- llhttp

## Maintenance note

This bundle reflects the dependency set relevant to the current project configuration. Python dependency resolution can change transitive package versions over time. Before publishing a release after dependency changes, compare the actual packaged environment against this directory and add, update, or remove notices as necessary.

A practical release check is to inspect the active build environment with `pip freeze` / package metadata and compare it with the PyInstaller output and this notice inventory.

The presence of a license text here does not imply endorsement by the corresponding project. This file is informational and is not legal advice.
