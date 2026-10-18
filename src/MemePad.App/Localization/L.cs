using System.Globalization;

namespace MemePad.App.Localization;

/// <summary>
/// UI strings in English and Russian. The language is picked once at startup (settings, else the system UI
/// language); XAML reads these via <c>{x:Static loc:L.Name}</c>.
/// </summary>
public static class L
{
    public static bool IsRussian { get; private set; }

    public static void Use(string? language)
    {
        var code = language ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        IsRussian = code.Equals("ru", StringComparison.OrdinalIgnoreCase);
    }

    private static string T(string en, string ru) => IsRussian ? ru : en;

    public static string AppName => "MemePad";
    public static string DefaultPage => T("General", "Общие");
    public static string Search => T("Search", "Поиск");
    public static string SpeakPlaceholder => T("Text to speak", "Текст для озвучки");
    public static string Speak => T("Speak", "Сказать");
    public static string ToLibrary => T("Add to library", "В библиотеку");
    public static string Settings => T("Settings", "Настройки");
    public static string AddPage => T("Add page", "Добавить страницу");
    public static string RenamePage => T("Rename page", "Переименовать страницу");
    public static string DeletePage => T("Delete page", "Удалить страницу");
    public static string PageName => T("Page name", "Название страницы");
    public static string FavoritesOnly => T("Favorites only", "Только избранное");
    public static string Mic => T("Microphone", "Микрофон");
    public static string Sounds => T("Sounds", "Звуки");
    public static string Stop => T("Stop", "Стоп");
    public static string Play => T("Play", "Играть");
    public static string AssignHotkey => T("Assign hotkey", "Назначить клавишу");
    public static string Edit => T("Edit", "Редактировать");
    public static string ToggleFavorite => T("Favorite", "В избранное");
    public static string Remove => T("Remove", "Удалить");
    public static string Favorite => T("In favorites", "В избранном");
    public static string DropHint => T("Drop audio files here: mp3, wav, ogg, flac", "Перетащите сюда звуки: mp3, wav, ogg, flac");
    public static string StatusRunning => T("Cable connected", "Кабель подключён");
    public static string StatusNoCable => T("Cable not found", "Кабель не найден");
    public static string StatusReconnecting => T("Reconnecting…", "Переподключение…");
    public static string StatusStopped => T("Starting…", "Запуск…");
    public static string FileMissing => T("File not found", "Файл не найден");
    public static string FileUnreadable => T("Cannot read file", "Не удалось прочитать");
    public static string SpeechTitle => T("Speech: “{0}”", "Озвучка: «{0}»");
    public static string ClipTitle => T("Clip {0:HH:mm:ss}", "Клип {0:HH:mm:ss}");
    public static string TrayOpen => T("Open", "Открыть");
    public static string TrayExit => T("Exit", "Выход");
    public static string EditTitle => T("Edit sound", "Редактирование звука");
    public static string TrimStart => T("Start", "Начало");
    public static string TrimEnd => T("End", "Конец");
    public static string Volume => T("Volume", "Громкость");
    public static string Preview => T("Preview", "Прослушать");
    public static string Save => T("Save", "Сохранить");
    public static string Cancel => T("Cancel", "Отмена");
    public static string HotkeyTitle => T("Hotkey for “{0}”", "Клавиша для «{0}»");
    public static string HotkeyPrompt => T("Press a key combination or mouse button 4/5", "Нажмите сочетание клавиш или кнопку мыши 4/5");
    public static string HotkeyHelp => T("Esc — cancel · Backspace — remove", "Esc — отмена · Backspace — снять");
    public static string HotkeyConflict => T("Already assigned to “{0}”. Reassign?", "Уже назначено на «{0}». Переназначить?");
    public static string Reassign => T("Reassign", "Переназначить");
    public static string SystemDefault => T("System default", "Системное по умолчанию");
    public static string Unavailable => T("{0} (unavailable)", "{0} (недоступно)");
    public static string NotSet => T("not set", "не задано");
    public static string SecDevices => T("Devices", "Устройства");
    public static string SecHotkeys => T("Hotkeys", "Клавиши");
    public static string SecPlayback => T("Playback", "Воспроизведение");
    public static string SecVoice => T("Voice", "Голос");
    public static string SecTts => T("Text to speech", "Озвучка");
    public static string SecGeneral => T("General", "Общее");
    public static string Microphone => T("Microphone", "Микрофон");
    public static string HearMyself => T("Hear sounds myself", "Слышать звуки самому");
    public static string MonitorDevice => T("Headphones", "Наушники");
    public static string MonitorVolume => T("Monitor volume", "Громкость для себя");
    public static string KeyStop => T("Stop all", "Стоп всё");
    public static string KeyNextPage => T("Next page", "Следующая страница");
    public static string KeyPrevPage => T("Previous page", "Предыдущая страница");
    public static string KeyRecord => T("Save last seconds", "Записать последние секунды");
    public static string KeyEffect => T("Voice effect on/off", "Эффект голоса вкл/выкл");
    public static string HistoryLength => T("Recording length, s", "Длина записи, с");
    public static string Swallow => T("Hide hotkeys from the game", "Перехватывать клавиши (игра их не увидит)");
    public static string PttEnable => T("Hold Discord push-to-talk while a sound plays", "Зажимать push-to-talk Discord, пока играет звук");
    public static string PttKey => T("Push-to-talk key", "Клавиша push-to-talk");
    public static string ModeInterrupt => T("New sound stops the previous one", "Новый звук обрывает предыдущий");
    public static string ModeOverlap => T("Sounds play on top of each other", "Звуки играют поверх друг друга");
    public static string Effect => T("Effect", "Эффект");
    public static string EffectOn => T("Effect enabled", "Эффект включён");
    public static string EffectNone => T("None", "Нет");
    public static string EffectHigher => T("Higher", "Выше");
    public static string EffectLower => T("Lower", "Ниже");
    public static string EffectRobot => T("Robot", "Робот");
    public static string TtsVoice => T("Voice", "Голос");
    public static string TtsRate => T("Speed", "Скорость");
    public static string Language => T("Language (after restart)", "Язык (после перезапуска)");
    public static string LangSystem => T("System", "Как в системе");
    public static string RunAsAdmin => T("Restart as administrator", "Перезапустить от администратора");
    public static string RunAsAdminHint => T("Needed only if a game runs as administrator and ignores hotkeys.", "Нужно, только если игра запущена от администратора и не реагирует на клавиши.");
    public static string Change => T("Change", "Изменить");
    public static string CableTitle => T("One step left: a free virtual cable", "Остался один шаг: бесплатный виртуальный кабель");
    public static string CableBody => T("For others to hear your sounds in Discord and games, MemePad sends your voice and sounds into the free VB-Audio Virtual Cable. Install it once, then press “Check again”.", "Чтобы звуки слышали в Discord и играх, MemePad отправляет ваш голос и звуки в бесплатный VB-Audio Virtual Cable. Установите его один раз и нажмите «Проверить снова».");
    public static string CableDownload => T("Download VB-Cable", "Скачать VB-Cable");
    public static string CableRetry => T("Check again", "Проверить снова");
    public static string CablePreviewNote => T("Until then you can still preview sounds in your headphones.", "А пока звуки можно слушать в наушниках.");
    public static string DiscordTipTitle => T("Cable connected", "Кабель подключён");
    public static string DiscordTip => T("In Discord → Voice & Video choose the microphone “CABLE Output (VB-Audio Virtual Cable)” and turn off noise suppression, echo cancellation and automatic gain — they cut memes as noise. Do the same in games.", "В Discord → «Голос и видео» выберите микрофон «CABLE Output (VB-Audio Virtual Cable)» и выключите шумоподавление, эхоподавление и автоусиление — они режут мемы как шум. В играх — так же.");
    public static string Got => T("Got it", "Понятно");
    public static string AlreadyRunning => T("MemePad is already running (see the tray).", "MemePad уже запущен (смотрите в трее).");
}
