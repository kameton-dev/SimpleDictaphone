[Русский](README.ru.md)

---

<h1 align="center">
  <br>
  <a href="https://github.com/kameton-dev/SimpleDictaphone"><img width="130" height="130" alt="Ы" src="./SimpleDictaphone/Assets/images/logo-simpledictaphone.png" />
</a>
  <br>
  <b>SimpleDictaphone</b>
  <br>
</h1>

**SimpleDictaphone** - A simple sound recording app. You can listen to audio files within the app and, if needed, save them to a folder on your phone. #SimpleDictaphone

## Features

- Record voice notes to WAV files (16-bit PCM, mono, 16 kHz)
- Recordings list grouped by date (today / yesterday / older)
- Playback with progress bar, elapsed / total time, pause and rewind
- Rename and delete recordings
- Export a recording to `Music/SimpleDictaphone` so it shows up in the phone's music library

## Requirements

- Windows Phone 8.1 device or emulator
- To build: Visual Studio 2013/2015 with the Windows Phone 8.1 SDK

## Build and run

1. Clone the repository:
   ```
   git clone https://github.com/kameton-dev/SimpleDictaphone.git
   ```
2. Open `SimpleDictaphone.sln` in Visual Studio.
3. Select the **Emulator** or **Device** target and press **F5**.

<!-- ## Project structure

| File / folder | Purpose |
|---|---|
| `RecordingsPage.xaml(.cs)` | List, playback, rename, delete and export of recordings |
| `Resources/AppResources.resx` | UI strings (one `.resx` per language) |
| `Assets/AppBar/` | Application bar icons |

TODO: add the recording page and any other files -->

## Localization

All user-facing strings live in `Resources/AppResources*.resx`. To add a language, copy `AppResources.resx` to `AppResources.<culture>.resx` (for example `AppResources.de-DE.resx`), translate the values and add the culture to the project's supported cultures.

## Storage

Recordings are stored in the app's isolated storage. The **Save** button copies a recording to `Music/SimpleDictaphone` on the phone.

## Contributing

Issues and pull requests are welcome. This is an early project, so expect rough edges.

## License

This project is distributed under the [**GNU General Public License v3.0**](LICENSE).
