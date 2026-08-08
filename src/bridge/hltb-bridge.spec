from __future__ import annotations

import os

from PyInstaller.utils.hooks import collect_data_files


bridge_root = os.path.abspath(SPECPATH)

entry_point = os.path.join(
    bridge_root,
    "hltb_bridge_entry.py",
)

datas = []

# fake_useragent uses packaged data resources.
# Explicitly collect them rather than depending on
# PyInstaller's automatic analysis.
datas += collect_data_files("fake_useragent")


a = Analysis(
    [entry_point],
    pathex=[bridge_root],
    binaries=[],
    datas=datas,
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)


pyz = PYZ(a.pure)


exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name="hltb-bridge",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=False,
    console=True,
)


coll = COLLECT(
    exe,
    a.binaries,
    a.datas,
    strip=False,
    upx=False,
    upx_exclude=[],
    name="hltb-bridge",
)