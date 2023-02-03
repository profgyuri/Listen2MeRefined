# Listen2MeRefined

## What's still needed for the release version?

 - Implementing playlists
 - Drag 'n drop from outside the application
 - Reworking the UI to make it similar to Steam's UI
 - UX/QOL improvements like:
   - Adding remaining and total time displays
   - "Running text" where the title or artist name is too long
   - More understandable, easier replacing the searchlist items to the playlist
   - Individually choosable colors OR color themes
 - Adding context menu to list items (to "jump here", scan individually, etc.)
 - Ability to clear playlist items
 - Status report for scanning
 - Performance improvements (starting the app should not freeze the computer at all (now it is 2-10 seconds))

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
