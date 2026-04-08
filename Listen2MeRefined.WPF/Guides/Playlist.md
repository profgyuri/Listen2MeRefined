# Playlist Feature Guide

This document explains how to use playlist support in Listen2MeRefined.

## Overview

Playlists let you organize songs without duplicating audio files.

- A playlist stores only:
  - `Name`
  - `Items` (song links)
- A song can belong to multiple playlists.
- Deleting a playlist does not delete songs from the database.
- Deleting a song from the database removes it from all playlists.

## Playlist Pane and Tabs

The right-side playlist area is tab-based.

- `Default` tab:
  - Always exists.
  - Receives songs sent from Search Results.
- Named playlist tabs:
  - Opened using the `+` button.
  - One tab per playlist.
  - Can be closed with the tab close button.

### Example: Open a Playlist Tab

1. Click `+` in the Playlist pane.
2. Select `Road Trip`.
3. A new `Road Trip` tab opens.
4. Click `x` on the tab to close it.

## Creating Playlists

You can create playlists from:

- Settings -> `Playlists` tab
- Song context menu -> `Add To New Playlist`

### Example: Create from Song Context Menu

1. In Search Results or in the Playlist pane, right-click a selected song.
2. Open `Add To New Playlist`.
3. Type `Warmup Set`.
4. Press `Enter`.
5. The playlist is created and selected songs are added.

## Renaming Playlists

Use Settings -> `Playlists`.

### Example: Rename

1. Open `Settings` -> `Playlists`.
2. Select `Warmup Set` from the list.
3. Change name to `Warmup 2026` in the name field.
4. Click `Rename`.

## Deleting Playlists

Use Settings -> `Playlists`.

### Example: Delete

1. Open `Settings` -> `Playlists`.
2. Select `Old Mix`.
3. Click the delete icon.
4. Playlist is removed; songs remain in the DB.

## Adding Songs to Playlists

Right-click in Search Results or Playlist pane.

- Each playlist appears as a checkable menu item.
- Checking adds selected songs to that playlist.
- Duplicate song entries are prevented.

### Example: Add Multiple Songs

1. In Search Results, Ctrl-select 3 songs.
2. Right-click selection.
3. Check `Gym`.
4. All 3 songs are added to `Gym` (if not already present).

## Removing Songs from Playlists

- In a named playlist tab:
  - Select one or more songs.
  - Use `Ctrl+Delete` or the remove action.
  - Songs are removed from that playlist only.
- In `Default` tab:
  - Removes from default queue/tab contents.
  - Does not delete songs from DB.

## Context Menu Membership Feedback

When exactly one song is selected:

- Context menu shows all playlists.
- Checked playlists indicate where that song currently exists.

When multiple songs are selected:

- You can still add all selected songs to a playlist.
- Removal is allowed from the currently active named playlist context.

## Search Results Transfer Behavior

Settings -> `Playlists` -> `Search to default tab`:

- `Move`: send songs to Default tab and remove them from Search Results.
- `Copy`: send songs to Default tab and keep them in Search Results.

### Example: Copy Mode

1. Set `Search to default tab` to `Copy`.
2. Select two search results.
3. Click `Add Selected / All to Playlist` in Search Results.
4. Songs appear in `Default`, and remain in Search Results.

## Notes

- Song order inside playlists is not guaranteed.
- Playlist names are unique (case-insensitive).
- Playlist tab names follow playlist names and update after rename.
