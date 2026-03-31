using System;
using System.IO;
using System.Text.Json;

namespace Scan_Background.Properties
{
    // Простая JSON-настройка приложения
    internal sealed class Settings
    {
        // Храним настройки рядом с исполняемым файлом (в папке приложения)
        private static string filePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public static string FilePath => filePath;

        public static Settings Default { get; } = Load();

        public bool RunInTray { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        public bool FirstRun { get; set; } = true;

        public Settings() { }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            // Пытаемся атомарно записать рядом с exe: сначала в temp-файл в той же папке, потом заменяем целевой файл
            try
            {
                var dir = Path.GetDirectoryName(filePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(dir);
                var tmp = Path.Combine(dir, Path.GetRandomFileName());
                File.WriteAllText(tmp, json);
                // Если файл уже существует, используем File.Replace для атомарной замены, иначе перемещаем
                if (File.Exists(filePath))
                {
                    File.Replace(tmp, filePath, null);
                }
                else
                {
                    File.Move(tmp, filePath);
                }
                return;
            }
            catch
            {
                // попытка записи в %AppData% как fallback (аналогично атомарно)
                try
                {
                    var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scan_Background");
                    Directory.CreateDirectory(dir);
                    var alt = Path.Combine(dir, "appsettings.json");
                    var tmp = Path.Combine(dir, Path.GetRandomFileName());
                    File.WriteAllText(tmp, json);
                    if (File.Exists(alt))
                    {
                        File.Replace(tmp, alt, null);
                    }
                    else
                    {
                        File.Move(tmp, alt);
                    }
                    return;
                }
                catch
                {
                    // окончательно молчим — вызов сохранения не должен ломать приложение
                }
            }
        }

        // Перезагрузить настройки из файла (если он есть)
        public void Reload()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null)
                    {
                        this.RunInTray = s.RunInTray;
                        this.NotificationsEnabled = s.NotificationsEnabled;
                        this.Debug = s.Debug;
                        this.FirstRun = s.FirstRun;
                    }
                    return;
                }

                // fallback: попробовать из %AppData%
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scan_Background");
                var alt = Path.Combine(dir, "appsettings.json");
                if (File.Exists(alt))
                {
                    var json = File.ReadAllText(alt);
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null)
                    {
                        this.RunInTray = s.RunInTray;
                        this.NotificationsEnabled = s.NotificationsEnabled;
                        this.Debug = s.Debug;
                        this.FirstRun = s.FirstRun;
                    }
                }
            }
            catch { }
        }

        private static Settings Load()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null) return s;
                }
            }
            catch { }
            return new Settings();
        }
    }
}
