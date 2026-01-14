using System.Collections.Generic;

namespace UniversalGameTrainer
{
    public static class LocalizedStrings
    {
        public static Dictionary<string, Dictionary<string, string>> GetStringDictionary()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                ["File"] = new Dictionary<string, string> { { "EN", "File" }, { "RU", "Файл" } },
                ["Attach"] = new Dictionary<string, string> { { "EN", "Attach" }, { "RU", "Подключиться" } },
                ["Settings"] = new Dictionary<string, string> { { "EN", "Settings" }, { "RU", "Настройки" } },
                ["Language"] = new Dictionary<string, string> { { "EN", "Language" }, { "RU", "Язык" } },
                ["Save"] = new Dictionary<string, string> { { "EN", "Save" }, { "RU", "Сохранить" } },
                ["Load"] = new Dictionary<string, string> { { "EN", "Load" }, { "RU", "Загрузить" } },
                ["Tabs"] = new Dictionary<string, string> { { "EN", "Tabs" }, { "RU", "Вкладки" } },
                ["AddTab"] = new Dictionary<string, string> { { "EN", "Add Tab" }, { "RU", "Добавить вкладку" } },
                ["RemoveTab"] = new Dictionary<string, string> { { "EN", "Remove Tab" }, { "RU", "Удалить вкладку" } },
                ["Ready"] = new Dictionary<string, string> { { "EN", "Ready" }, { "RU", "Готово" } },
                ["NotReady"] = new Dictionary<string, string> { { "EN", "Not Ready" }, { "RU", "Не готово" } },
                ["Attached"] = new Dictionary<string, string> { { "EN", "Attached to" }, { "RU", "Подключен к" } },
                ["ProcessNotFound"] = new Dictionary<string, string> { { "EN", "Process not found" }, { "RU", "Процесс не найден" } },
                ["AttachError"] = new Dictionary<string, string> { { "EN", "Attach error" }, { "RU", "Ошибка подключения" } },
                ["Enabled"] = new Dictionary<string, string> { { "EN", "Enabled" }, { "RU", "Включено" } },
                ["PointerConfiguration"] = new Dictionary<string, string> { { "EN", "Pointer Configuration" }, { "RU", "Конфигурация указателя" } },
                ["KeyBlocking"] = new Dictionary<string, string> { { "EN", "Key Blocking" }, { "RU", "Блокировка клавиш" } },
                ["MacroConfiguration"] = new Dictionary<string, string> { { "EN", "Macro Configuration" }, { "RU", "Конфигурация макросов" } },
                ["Module"] = new Dictionary<string, string> { { "EN", "Module" }, { "RU", "Модуль" } },
                ["BaseOffset"] = new Dictionary<string, string> { { "EN", "Base Offset" }, { "RU", "Базовое смещение" } },
                ["Offsets"] = new Dictionary<string, string> { { "EN", "Offsets" }, { "RU", "Смещения" } },
                ["TriggerValue"] = new Dictionary<string, string> { { "EN", "Trigger Value" }, { "RU", "Значение триггера" } },
                ["ReadIntervalMs"] = new Dictionary<string, string> { { "EN", "Read Interval (ms)" }, { "RU", "Интервал чтения (мс)" } },
                ["BlockDurationMs"] = new Dictionary<string, string> { { "EN", "Block Duration (ms)" }, { "RU", "Длительность блокировки (мс)" } },
                ["KeysToBlock"] = new Dictionary<string, string> { { "EN", "Keys to Block" }, { "RU", "Клавиши для блокировки" } },
                ["Add"] = new Dictionary<string, string> { { "EN", "Add" }, { "RU", "Добавить" } },
                ["Remove"] = new Dictionary<string, string> { { "EN", "Remove" }, { "RU", "Удалить" } },
                ["DelayAfterTriggerMs"] = new Dictionary<string, string> { { "EN", "Delay After Trigger (ms)" }, { "RU", "Задержка после триггера (мс)" } },
                ["EnableMacro"] = new Dictionary<string, string> { { "EN", "Enable Macro" }, { "RU", "Включить макрос" } },
                ["RecordMacro"] = new Dictionary<string, string> { { "EN", "Record Macro" }, { "RU", "Записать макрос" } },
                ["PlayMacro"] = new Dictionary<string, string> { { "EN", "Play Macro" }, { "RU", "Воспроизвести макрос" } },
                ["EditMacro"] = new Dictionary<string, string> { { "EN", "Edit Macro" }, { "RU", "Редактировать макрос" } },
                ["SaveConfig"] = new Dictionary<string, string> { { "EN", "Save Config" }, { "RU", "Сохранить конфиг" } },
                ["LoadConfig"] = new Dictionary<string, string> { { "EN", "Load Config" }, { "RU", "Загрузить конфиг" } },
                ["ConfigurationSaved"] = new Dictionary<string, string> { { "EN", "Configuration saved successfully" }, { "RU", "Конфигурация успешно сохранена" } },
                ["ConfigurationLoaded"] = new Dictionary<string, string> { { "EN", "Configuration loaded successfully" }, { "RU", "Конфигурация успешно загружена" } },
                ["RecordMacroPlaceholder"] = new Dictionary<string, string> { { "EN", "Macro recording functionality would be implemented here" }, { "RU", "Функция записи макроса будет реализована здесь" } },
                ["PlayMacroPlaceholder"] = new Dictionary<string, string> { { "EN", "Macro playback functionality would be implemented here" }, { "RU", "Функция воспроизведения макроса будет реализована здесь" } },
                ["EditMacroPlaceholder"] = new Dictionary<string, string> { { "EN", "Macro editor functionality would be implemented here" }, { "RU", "Функция редактора макроса будет реализована здесь" } },
                ["AttachToProcess"] = new Dictionary<string, string> { { "EN", "Attach to Process" }, { "RU", "Присоединиться к процессу" } },
                ["ExeName"] = new Dictionary<string, string> { { "EN", "EXE Name" }, { "RU", "Имя EXE" } },
                ["AvailableProcesses"] = new Dictionary<string, string> { { "EN", "Available Processes" }, { "RU", "Доступные процессы" } },
                ["Cancel"] = new Dictionary<string, string> { { "EN", "Cancel" }, { "RU", "Отмена" } },
                ["LastExeName"] = new Dictionary<string, string> { { "EN", "Last EXE Name" }, { "RU", "Последнее имя EXE" } }
            };
        }
    }
}