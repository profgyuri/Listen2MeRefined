# Developers Guide: `NAudioMusicPlayer` Abstractions

This document explains **why** `NAudioMusicPlayer` was split into abstractions and **how** those abstractions collaborate at runtime.

## Why these abstractions exist

`NAudioMusicPlayer` used to own most playback concerns directly (track loading, output-device setup, progress checks, and error fallback). That made it harder to:

- reason about state transitions,
- handle errors consistently,
- test orchestration behavior in isolation.

The current structure keeps `NAudioMusicPlayer` as an **orchestrator** and delegates narrow responsibilities to focused collaborators.

## Core collaborators

### `ITrackLoader`

Purpose:

- Load a track and return a structured result (`TrackLoadResult`) instead of throwing/handling everything inside the player.

Responsibilities:

- Check whether a file is missing.
- Create the correct NAudio reader.
- Detect unsupported/corrupt inputs and report status (`MissingFile`, `UnsupportedFormat`, `CorruptFile`).

Why it helps:

- Unsupported/missing/corrupt handling can follow one skip-and-next path in the player.

---

### `IPlaybackOutput`

Purpose:

- Abstract the playback sink (`WaveOutEvent`) and output-device reinitialization lifecycle.

Responsibilities:

- Play/Pause/Stop and Volume operations.
- Reinitialize output for a `WaveStream` + selected device.
- Return a typed reconfigure result (`PlaybackOutputReconfigureResult`) for fallback decisions.

Why it helps:

- Error handling for output init is centralized and explicit.
- The player no longer needs direct `WaveOutEvent` lifecycle logic.

---

### `IPlaybackProgressMonitor`

Purpose:

- Encapsulate end-of-track detection heuristics.

Responsibilities:

- Keep internal progress sampling state.
- Decide whether playback should auto-advance.
- Reset on track/state changes.

Why it helps:

- Playback boundary logic is reusable and independently testable.
- Keeps orchestration code in `NAudioMusicPlayer` small.

## `NAudioMusicPlayer` orchestration model

`NAudioMusicPlayer` coordinates playback with a minimal internal state model:

- `Stopped`
- `Playing`
- `Paused`

### High-level flow

1. UI/control calls (`PlayPauseAsync`, `NextAsync`, etc.) enter the player.
2. Track selection is resolved from `IPlaybackQueueService`.
3. Track loading is delegated to `ITrackLoader`.
4. On success:
   - publish `CurrentSongNotification`,
   - reset progress monitor,
   - reconfigure output via `IPlaybackOutput`.
5. On load failure:
   - remove the track,
   - retry from current queue position through one shared unplayable-track path.

## Device changes: one reconfiguration path

`Handle(AudioOutputDeviceChangedNotification, ...)` uses the same reconfigure logic used elsewhere.

Key goals:

- preserve playback timestamp,
- preserve play/pause intent,
- avoid duplicated stop/start side effects.

If reconfiguration fails, fallback is based on typed result data:

- if previous output can be preserved, keep the safer existing output;
- otherwise transition to safe stopped behavior.

## Testing strategy enabled by these abstractions

The abstractions make orchestration tests straightforward using mocks/fakes:

- device change while playing vs paused,
- unsupported format path,
- missing file removal + retry,
- progress monitor end-of-track boundaries.

Tests can focus on decision-making in the player rather than NAudio internals.

## Dependency injection wiring

`MusicPlayerModule` registers:

- `NAudioMusicPlayer` as controller + implemented interfaces,
- `ITrackLoader` -> `NAudioTrackLoader`,
- `IPlaybackOutput` -> `WaveOutPlaybackOutput`,
- `IPlaybackProgressMonitor` -> `PlaybackProgressMonitor`.

This keeps runtime composition explicit and allows implementation swaps later.

## Practical guidance for future changes

- Add new format handling in `ITrackLoader` implementations first.
- Keep `NAudioMusicPlayer` focused on orchestration and state transitions, not low-level I/O.
- Extend `PlaybackOutputReconfigureResult` rather than adding broad catch-all logic in the player.
- Prefer adding unit tests around orchestration contracts when introducing new behavior.
