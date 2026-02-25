# Listen2MeRefined

MIT License as per the License.md

Welcome to Listen2Me, a music player app aimed for electronic music! The project is currently open source, all written in C#.

Before you get any ideas, it is not meant to challange AIMP, ITunes, or whatever app you use for playing your music. But who knows, what the future holds.

This project was initially created to accomodate my needs in a music player app. I prefer hard dance music in almost every form, but this does not mean, that your favorite rock music cannot be played with this app (as long as you have a copy of it on your computer). It is still under development, but 1.0 is on the horizon!

Important to note, that due to some of the used dependencies, this is a __Windows only__ app - at least for now.

You can always find the **latest release** [here](https://github.com/profgyuri/Listen2MeRefined/releases)

## Features

- All the necessary playback controls (previous, next, play, pause, stop, volume)
- Shuffling - the new order of the playlist is visible,  so you can reorder it if you'd like
- Quick search
- Advanced search via a criterion builder - filter trough every metadata that is currently supported
- Thourough settings menu - from font styles, folder management, small window settings
- Small window displaying the currently playing song in all 4 corners of your window
- Drag 'n drop support (currently only within the app)

## Quick Start

By simply opening the app, nothing will happen, but here is a basic process to get you started:

1. Open settings from the top right corner.
2. Go to the 'Library' tab and add 1 of your music folders via the file browser.
3. Now you may see a 'Scanning' status on the top of the main window. When it reaches 100%, you are free to proceed.
4. At the bottom of the window you will find the search bar. Either click on the search button without any text provided, or write there something to apply it as a filter.
5. The search results are shown on the left side. Above them there is an arrow pointing to the right. Click on that to move the results to the playlist.
6. Double click on any song and use your media keys, or the buttons on the UI to start the playback.

## Feature request and bug report

You can open a new [issue](https://github.com/profgyuri/Listen2MeRefined/issues) at any time, user created issues will always be prioritized.

**Report a bug**: The title should be starting with [BUG] and a short description about what is not working as expected, the description should contain exact steps to reproduce, and what you expected to happen. 

**Request a feature**: The title should be starting with [FEATURE], and then the name of your feature. Write down in the description in a list, what behaviour are you expecting in what scenarios. The better the description is, the faster I can get to it. 

Examples:

[BUG] Shuffle button does not shuffle

[[FEATURE] Mobile companion app](https://github.com/profgyuri/Listen2MeRefined/issues/80)

## Contribution

I try to keep the code as simple as I can, but that doesn't mean there are no hundreds of lines in a file. This is mainly true for viewmodel files, since I don't want to add extra confusion with self-made partial classes. Sorry, if it differs from your preferences.

**Some** harder concepts are documented in the [wiki](https://github.com/profgyuri/Listen2MeRefined/wiki).

### First time contribution?

If this would be your first time working on  someone else's repo and you feel overwhelmed, a good entry point is to fix some [sonar issues](https://sonarcloud.io/project/issues?issueStatuses=OPEN%2CCONFIRMED&id=profgyuri_Listen2MeRefined). The site explains what the problem is and how you could solve it. It is simple, but still looks good on your resume.

### ViewModels

Please keep in mind, that the existing viewmodels are using the CommunityToolkit. This means if you plan to contribute, use ```ObservablePropertyAttribute``` on private fields if you need a bindable property (some exceptions may apply), and ```RelayCommandAttribute``` on methods to use as bindable commands. All viewmodels have to inherit from ```ViewModelBase```, and if there is any text on it's UI, also from ```INotificationHandler<FontFamilyChangedNotification>```.

If you have any long running initialization tasks, that have to be run on a background, override ```InitializeCoreAsync```. See an existing viewmodel for usage.

### General

- If you have to create a new class, also create the interface for its public methods and properties. Naming should be straightforward, see: ```IMusicPlayer``` -> ```NAudioMusicPlayer``` or ```IFileTransmitter``` -> ```FileTransmitter```
- Add unit tests in the Tests project to cover at least 50% of the submitter code. You can use [coverlet](https://github.com/coverlet-coverage/coverlet) to check if you have enough coverage.
- Always place the test file in the same structure, as you can see it in the Infrastructure project. *SameFolder*/*SameName*Tests.cs
- The project uses Autofac modules for DI in the WPF project. Either use the best representing, currently existing module for your registrations, or create a new one.
- Logging is mandatory in viewmodels, but lower layers can log events too, if it makes sense. The used framework is Serilog.
- If you use AI agents, double check the work they do!
- Please give a proper explanation of your changes in the PR comment, so I know, what I should try out before merging.

For any open work to do, see either the [issues](https://github.com/profgyuri/Listen2MeRefined/issues), or [projects](https://github.com/users/profgyuri/projects/1) page of the repo. Give a sign in any form, that you intend to start working on a problem.

## Roadmap

There is no specific roadmap. I try to implement things as I see fit, but now it is time to push for 1.0

### What's still needed for 1.0?

 - Implementing playlists
 - Drag 'n drop from outside the application
 - UX/QOL improvements like:
   - Individually choosable colors OR color themes
 - Adding context menu to list items (to "jump here", scan individually, etc.) (properly on right click)
 - Performance improvements
   - Registering global keyboard and mouse hooks
   - Enumerating audio output devices
   - Sliders feel janky
 - Localization (hungarian and german planned, help needed with other languages if they will be requested)
 - and many more

See [this milestone](https://github.com/profgyuri/Listen2MeRefined/milestone/1) for the progress.

## Known issues

 - Multiple display setup is currently not supported in any form. (Fix is not planned as per now.)
 - Exstensible .wav files are skipped both in analyzing, or if already analyzed, then from playing

## Support

If you like what I've done so far, or just believe in the project, feel free to let me know with any amount of donation. This would certainly make me push harder.
