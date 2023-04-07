# Listen2MeRefined

## Quick Starting Guide

As per now, after releasing version 0.5.0, the first start of the application will create a database file (listen2me.db). This file will contain your settings, and also the metadata stored on your music files.

At the first startup you have to let the application know, where your msuic files live on your system.
First, head to the settings, found in the top right corner:

![Settings-circled](https://user-images.githubusercontent.com/44247462/224454100-4d88627b-1715-4934-9d56-7eb43de0f74e.jpg)

Now, that the settings window is open, you can add you music folders at the 'Local Folders' option, clicking the 'Browse' button.

![Local-Folders-Browse-circled](https://user-images.githubusercontent.com/44247462/224454412-55b4d97b-ca4c-413b-8f08-08cfe7c11ca3.jpg)

This opens a folder browser window. After you navigated to your music folder either with going the the filesystem, or simply pasting the path at the botton of the window, hit the 'Select' button to add the folder to the database.

![Folder-Browser](https://user-images.githubusercontent.com/44247462/224454709-a69a6985-4fe0-4e5d-a137-c6634eaa9b92.jpg)

If you have selected a folder from the available list, it will be added to the end of the path, if you press 'Select', otherwise you can see the path that will be added to your local folders list. 

After adding one of your folders, the program will automatically scan for your supported music files. The scanning is not recursive, meaning subfolders will not be scanned. 
As per now, there is no visual feedback for the scanning progress, so for a baseline, I could scan 1000 songs in around 15 seconds with an AMD Ryzen 5800x cpu. The scanning happens in alphabetic order, so if you can see the song that is alphabetically last in the folder, then everything is set.

### I added my folder, but i can't see my songs?

To keep the cpu load relatively low during development, your songs won't show up automatically. You have to initiate a search.

![Searchbar](https://user-images.githubusercontent.com/44247462/224455658-d4b11a73-5393-4535-9b6a-37a1218092fd.jpg)

You can find the searchbar at the bottom of the maind window. The first part is an input field, which is basically a filter for your quick search. If you leave this field empty, every song that was scanned will show up in the 'Search Results'. However, if you want to use a filter, the program will search for it in every supported metadata of your scanned files, including the path. If there is at least 1 metadata, that contains your keyword (which can be a number), the song will show up in the search results, but only after you hit enter, or clikced on the quick search button (represented by a magnifying glass).

There is also an option for advanced searching, right next to the magnifying glass (shown as an opened book). This will open up the advanced search window.

![Advanced-Search](https://user-images.githubusercontent.com/44247462/224456586-8c2e9fc4-a1d7-40e8-a7dc-104a3ef3c36d.jpg)

Here you can set up as many filters as you want. You also have the option to include, exclude, a specific filter, or make the exact. After you set up your filters, you can choose, if you want any of them to match within a song's metadata, or to match every single filter you set up on every song that should be returned to the search results. For this to work properly, it is recommended to set up the artists, title, genre, bpm, etc. in every files metadata, because if the values are empty, then there is simply nothing to search for. 

Now that you have your songs in the search results list, you can simply drag and drop them to the playlist, hit ctrl + right arrow key, or use the button next to the 'Search Results' text. If you have nothing selected, then every item from the search results will be replaced to the playlist, otherwise only the selected songs will be added.

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
 - Removing the currently playing song from the playlist is not refreshing the index, resulting in skipping 1 song
