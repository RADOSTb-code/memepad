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
    public static string AlreadyRunning => T("MemePad is already running (see the tray).", "MemePad уже запущен (смотрите в трее).");
}
