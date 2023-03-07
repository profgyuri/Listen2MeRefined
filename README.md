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
 - Scrollbar is still not visible
 - Shuffling can cause a crash when the currently loaded song is not in the playlist
 - Crashing when the selected folder does not contain music files
