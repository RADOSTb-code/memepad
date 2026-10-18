# MemePad

**Бесплатная звуковая панель для Windows с открытым кодом.** Назначьте мемам горячие клавиши, нажмите в игре или в Discord — и звук уйдёт в микрофон вместе с вашим голосом.

[English below](#english)

## Что умеет

- Библиотека звуков: перетащите файлы mp3, wav, ogg или flac в окно.
- Глобальные горячие клавиши, в том числе кнопки мыши 4 и 5, работают поверх полноэкранных игр. Есть клавиша «Стоп».
- Несколько страниц клавиш, например отдельные наборы для разных игр.
- Push-to-talk: пока играет звук, MemePad сам держит вашу клавишу PTT из Discord.
- Звуки можно слушать самому в наушниках, с отдельной громкостью.
- Озвучка текста голосами Windows.
- «Запиши, что было»: клавиша сохраняет последние 10–120 секунд звука с компьютера как новый мем.
- Обрезка и громкость для каждого звука; исходный файл не меняется.
- Эффекты голоса: выше, ниже, робот.
- Поиск, избранное, интерфейс на русском и английском.

Всё бесплатно: без подписок, рекламы и телеметрии.

## Установка

1. Установите бесплатный **[VB-Audio Virtual Cable](https://vb-audio.com/Cable/)** и перезагрузите компьютер, если установщик попросит. MemePad смешивает ваш голос и звуки и отправляет результат в этот кабель.
2. Скачайте `MemePad-…-win-x64.zip` со страницы Releases, распакуйте его и запустите `MemePad.exe`. Устанавливать .NET не нужно.
3. В **Discord → Настройки → Голос и видео** выберите устройство ввода **CABLE Output (VB-Audio Virtual Cable)**. В играх сделайте то же самое.
4. Там же **выключите шумоподавление, эхоподавление и автоматическую регулировку усиления**. Иначе Discord будет принимать мемы за шум и вырезать их.

MemePad должен работать, пока вы говорите. Закрытие окна сворачивает его в трей, а выход — через меню значка в трее.

### Если клавиши не срабатывают в игре

Если игра запущена от имени администратора, обычные программы не видят её нажатий. Откройте **Настройки → Перезапустить от администратора**.

## Где хранятся данные

Настройки лежат в `%APPDATA%\MemePad\settings.json`, клипы и озвучки — рядом, в папках `Recordings` и `Speech`. Звуки хранятся ссылками на ваши файлы. Переменная окружения `MEMEPAD_DATA_DIR` переносит данные в другую папку, это удобно для портативной версии.

## Сборка из исходников

Нужен [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test
dotnet run --project src/MemePad.App
```

Релизный архив собирается командой `pwsh scripts/publish.ps1 -Version 0.1.0`. Проверить отправку звука в кабель можно командой `MemePad.exe --selftest`: она проигрывает тон 1 кГц в течение секунды, а результат пишет в `%TEMP%\memepad-selftest.log`.

## Лицензия и отказ от ответственности

MIT, см. [LICENSE](LICENSE). MemePad — независимый проект. Он не связан с Leppsoft, Soundpad или VB-Audio и не использует их код, звуки и ресурсы. VB-Cable распространяется его авторами на их собственных условиях.

---

## English

**A free, open-source soundboard for Windows.** Bind memes to hotkeys, press one in a game or Discord, and the sound goes out through your microphone together with your voice.

### Features

- Sound library: drag in mp3, wav, ogg or flac files.
- Global hotkeys, including mouse buttons 4 and 5, that work over fullscreen games, plus a Stop key.
- Several hotkey pages, for example one set per game.
- Push-to-talk: MemePad holds your Discord PTT key while a sound plays.
- Hear the sounds yourself in your headphones, with a separate volume.
- Text to speech with the Windows voices.
- "Save what just happened": a hotkey saves the last 10–120 seconds of your PC's audio as a new meme.
- Per-sound trim and volume; the original file is never changed.
- Voice effects: higher, lower, robot.
- Search, favorites, and an English or Russian UI.

No subscriptions, ads or telemetry.

### Setup

1. Install the free **[VB-Audio Virtual Cable](https://vb-audio.com/Cable/)**. MemePad mixes your voice with the sounds and plays the result into it.
2. Unzip the release and run `MemePad.exe`. No .NET install is needed.
3. In **Discord → Settings → Voice & Video**, set the input device to **CABLE Output (VB-Audio Virtual Cable)**. Do the same in your games.
4. **Turn off noise suppression, echo cancellation and automatic gain control** for that input, or Discord will treat memes as noise and cut them.

Keep MemePad running while you talk. Closing the window sends it to the tray; quit it from the tray menu.

If a game runs as administrator and ignores your hotkeys, use **Settings → Restart as administrator**.

### Build

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Run `dotnet test`, then `dotnet run --project src/MemePad.App`. For a release zip, run `pwsh scripts/publish.ps1`.

### License

MIT. MemePad is an independent project. It is not affiliated with Leppsoft, Soundpad or VB-Audio, and uses none of their code, sounds or assets.
