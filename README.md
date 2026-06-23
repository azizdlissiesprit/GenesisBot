# GenesisBot

A Discord music bot built with **.NET 8**, **Discord.Net**, and **Victoria** (a Lavalink client). It supports both classic prefix commands (`!play`) and Discord slash commands (`/play`), and streams music from **SoundCloud** (songs, search, playlists). YouTube links are accepted too — the bot reads the title and plays the matching SoundCloud track.

## Features

- 🎵 Play by **song name**, **SoundCloud URL**, **SoundCloud playlist URL**, or **YouTube URL**
- ⚡ Works as both **`!` text commands** and **`/` slash commands**
- 📜 Queue with auto-advance, skip, pause/resume, volume, now-playing

## Commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `play <song / URL>` | `p` | Play a track or add it to the queue |
| `join` | `connect` | Join your voice channel |
| `leave` | `disconnect`, `dc` | Leave the voice channel |
| `pause` | | Pause the current track |
| `resume` | | Resume playback |
| `skip` | `next`, `s` | Skip to the next track |
| `stop` | | Stop playback and clear the queue |
| `queue` | `q` | Show the current queue |
| `nowplaying` | `np`, `current` | Show the current track |
| `volume <0-150>` | `vol` | Set the volume |
| `ping` | | Check bot latency |
| `help` | | List all commands |
| `info` | | Bot information |

All commands work with either the `!` prefix or as `/` slash commands.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Java 17+](https://adoptium.net/) (to run Lavalink)
- A Discord bot token — create one at the [Discord Developer Portal](https://discord.com/developers/applications). Enable the **Message Content Intent**, and invite the bot with the `bot` **and** `applications.commands` scopes.

## Setup

### 1. Lavalink (the audio server)

1. Download `Lavalink.jar` from the [Lavalink releases](https://github.com/lavalink-devs/Lavalink/releases) (v4.2.0+).
2. Put it in a folder and copy [`lavalink/application.example.yml`](lavalink/application.example.yml) next to it as `application.yml`.
3. Start it (leave it running):
   ```bash
   java -jar Lavalink.jar
   ```
   Wait for **"Lavalink is ready to accept connections."**

### 2. Bot config

Copy the example config and add your token:

```bash
# from the repo root
cp GenesisBot/Config/config.example.json GenesisBot/Config/config.json
```

Then edit `GenesisBot/Config/config.json` and replace `YOUR_DISCORD_BOT_TOKEN` with your real token. (This file is git-ignored so your token never gets committed.)

### 3. Run the bot

```bash
cd GenesisBot
dotnet run
```

Wait for **"Bot is connected and ready!"**, then use `/play` or `!play` in your server.

## Notes

- **Audio source:** the bot plays from **SoundCloud**, which works without any API keys. A plain song name does a SoundCloud search; SoundCloud playlist/track URLs load directly.
- **YouTube:** pasting a YouTube link works by reading the video's title and playing the SoundCloud match — YouTube's own playback is currently broken in the underlying plugin (a signature-cipher change with no released fix). If/when that's fixed, switching back is a one-line change in `MusicService.cs` (`scsearch:` → `ytsearch:`).
- **SoundCloud "Go+" tracks:** some official-artist uploads only stream a ~30s preview; the bot prefers full user re-uploads to avoid those.

## Tech stack

- .NET 8 · Discord.Net 3.19 · Victoria 7.0.6 (Lavalink client) · Lavalink 4.2.x
