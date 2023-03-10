# Listen2MeRefined

## What's still needed for the release version?

 - Implementing playlists
 - Drag 'n drop from outside the application
 - Reworking the UI to make it similar to Steam's UI (~75% done)
 - UX/QOL improvements like:
   - Adding remaining and total time displays
   - "Running text" where the title or artist name is too long (90% done but laggy experience)
   - Individually choosable colors OR color themes
 - Adding context menu to list items (to "jump here", scan individually, etc.)
 - Status report for scanning
 - Performance improvements (registering global keyboard and mouse hooks is still a problem)

## Working features

 - Audio playing in general
 - Quick search
 - Advanced search
 - Waveform as progress bar during playback
 - All available options are working
   - Adding local folders for scanning
   - Scan on startup or only manually
   - Change fontstyle
   - Change audio output device

## Known issues

 - Multiple display setup is currently not supported in any form.
 - Exstensible .wav files are skipped both in analyzing, or if already analyzed, then from playing
 - Removing a music folder does not remove the songs it containes from the database
 - Scanning on startup does not seem to be working (or atleast the deletion does not happen where it would be applicable)
 - Removing the currently playing song from the playlist is not refreshing the index, resulting in skipping 1 song
