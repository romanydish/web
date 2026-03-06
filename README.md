# IPTV Player Pro (WPF + LibVLCSharp)

A professional IPTV desktop player architecture for Windows, inspired by IPTV Smarters and Tivimate.

## Highlights

- WPF + MVVM project structure.
- LibVLCSharp-based playback with hardware acceleration flags.
- M3U parser for URL and local file imports.
- Xtream Codes API integration for live, VOD, and series loading.
- XMLTV EPG loader and current/next program surfacing.
- Favorites, categories, search, channel zapping, and volume controls.
- Local proxy relay system for stream URL masking.
- Secure local credential storage via DPAPI.
- Token service for optional stream gatekeeping.

## Project Structure

- `src/IPTVPlayer.App/Player` - Playback engine integrations.
- `src/IPTVPlayer.App/Parsers` - M3U parsing.
- `src/IPTVPlayer.App/Api` - Xtream API client.
- `src/IPTVPlayer.App/Epg` - XMLTV parsing and mapping.
- `src/IPTVPlayer.App/Proxy` - Local proxy relay service.
- `src/IPTVPlayer.App/Security` - Secure storage and tokenization.
- `src/IPTVPlayer.App/ViewModels` - MVVM orchestration.
- `src/IPTVPlayer.App/Views` - WPF user interface.

## Build

Open `IPTVPlayer.sln` in Visual Studio 2022 on Windows and build with .NET 8 SDK.

## Notes

This codebase is designed as a production-style foundation and can be extended with:

- PIP and multi-screen windows.
- DVR recording pipelines.
- Advanced subtitle management.
- Full timeline EPG grid UX.
- Backend-authenticated secure proxy services.
