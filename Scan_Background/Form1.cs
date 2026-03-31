using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace Scan_Background
{
    public partial class Form1 : Form
    {
        private bool exiting = false;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.Timer checkTimer;
        private string watchPath = "\\\\192.168.3.7\\Users\\user\\Documents\\Общая папка\\Драйвера\\scan\\go.scan";
        private bool notified = false;
        private bool scanning = false;

        public Form1()
        {
            InitializeComponent();
            InitializeTray();
            InitializeTimer();
            LoadSettings();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Показываем окно настроек при первом запуске (если нет сохранённых настроек)
            // Если настроено запускать в трее и пользователь уже применил — уходим в трей.
            if (Properties.Settings.Default.FirstRun)
            {
                Properties.Settings.Default.FirstRun = false;
                Properties.Settings.Default.Save();
                return; // показываем окно
            }

            if (Properties.Settings.Default.RunInTray)
            {
                HideToTray();
            }
        }

        // При первом показе формы — если настроено сворачивание в трей, прячем окно
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                if (Properties.Settings.Default.RunInTray)
                {
                    // Скрываем окно и убираем таскбар чтобы при автозапуске приложение фактически запускалось в трее
                    this.ShowInTaskbar = false;
                    this.WindowState = FormWindowState.Minimized;
                    HideToTray();
                }
            }
            catch { }
        }

        // Инициализация компонентов трея и контекстного меню
        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem("Показать окно");
            showItem.Click += (s, e) => { ShowFromTray(); };
            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) => { exiting = true; trayIcon.Visible = false; Application.Exit(); };
            trayMenu.Items.AddRange(new ToolStripItem[] { showItem, exitItem });

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Scan Watcher";
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseUp += TrayIcon_MouseUp;
        }

        // Реакция на клики мыши по иконке
        private void TrayIcon_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Контекстное меню откроется автоматически
            }
            else if (e.Button == MouseButtons.Left)
            {
                // По левому клику показываем окно
                ShowFromTray();
            }
        }

        // Показать окно из трея
        private void ShowFromTray()
        {
            this.Invoke(() =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            });
        }

        // Скрыть окно и оставить только иконку
        private void HideToTray()
        {
            this.Invoke(() =>
            {
                this.Hide();
            });
        }

        // Настройка таймера проверки файла
        private void InitializeTimer()
        {
            checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 5000; // 5 секунд
            checkTimer.Tick += CheckTimer_Tick;
            checkTimer.Start();
        }

        // Обработчик тика: проверяем наличие файла
        private void CheckTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                bool exists = File.Exists(watchPath);
                if (exists && !notified)
                {
                    notified = true;
                    // Запускаем процесс сканирования в фоне, если он ещё не запущен
                    if (!scanning)
                    {
                        try { runScan(); } catch { }
                    }
                    if (Properties.Settings.Default.NotificationsEnabled)
                    {
                        ShowNotification("Файл обнаружен", $"Файл найден: {watchPath}");
                    }
                    if (Properties.Settings.Default.Debug)
                    {
                        // Лог и уведомление для отладки
                        DebugWrite($"Файл найден: {watchPath}");
                    }
                }
                else if (!exists)
                {
                    // Сбрасываем флаг, чтобы при следующем появлении снова уведомить
                    notified = false;
                    if (Properties.Settings.Default.Debug)
                    {
                        // В режиме отладки сообщаем также о том, что файл отсутсвует
                        DebugWrite($"Файл НЕ обнаружен: {watchPath}");
                        if (Properties.Settings.Default.NotificationsEnabled)
                        {
                            // Показываем уведомление только если включены уведомления и отладка
                            ShowNotification("Файл не обнаружен", $"Файл не найден: {watchPath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Properties.Settings.Default.Debug)
                {
                    DebugWrite(ex.ToString());
                }
            }
        }

        // Показать стандартное уведомление винды
        private void ShowNotification(string title, string text)
        {
            // Используем NotifyIcon для показа подсказки
            trayIcon.BalloonTipTitle = title;
            trayIcon.BalloonTipText = text;
            trayIcon.ShowBalloonTip(3000);
        }

        // Лёгкий лог в файл для отладки
        private async System.Threading.Tasks.Task WriteLogAsync(string text)
        {
            try
            {
                var log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scan_debug.log");
                var line = DateTime.Now.ToString("s") + " " + text + Environment.NewLine;
                // Используем FileStream для безопасного добавления и возможности читать файл параллельно
                try
                {
                    using (var stream = new FileStream(log, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(line).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Если не удалось через FileStream, пробуем простую запись
                    try { File.AppendAllText(log, line); } catch { }
                }

                if (Properties.Settings.Default.Debug)
                {
                    // Обновляем UI в UI-потоке
                    AppendLog(line);
                }
            }
            catch { }
        }

        // Файловая запись логов вызывается неблокирующе
        private void DebugWrite(string text)
        {
            // fire-and-forget
            _ = WriteLogAsync(text);
        }

        // Добавляем строку в окно логов (в UI)
        private void AppendLog(string line)
        {
            try
            {
                if (txtLog == null) return;
                this.Invoke(() =>
                {
                    if (!txtLog.Visible) txtLog.Visible = true;
                    txtLog.AppendText(line);
                    txtLog.ScrollToCaret();
                });
            }
            catch { }
        }

        // Загрузка настроек в чекбоксы
        private void LoadSettings()
        {
            try
            {
                // Просим Settings перезагрузиться — он сам попробует сначала рядом с exe, затем в %AppData%
                Properties.Settings.Default.Reload();

                // Применяем значения к UI
                if (chkRunInTray != null) chkRunInTray.Checked = Properties.Settings.Default.RunInTray;
                if (chkNotifications != null) chkNotifications.Checked = Properties.Settings.Default.NotificationsEnabled;
                if (chkDebug != null) chkDebug.Checked = Properties.Settings.Default.Debug;

                // Лог — откуда были загружены настройки
                var exePath = Scan_Background.Properties.Settings.FilePath;
                var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Scan_Background");
                var appDataPath = Path.Combine(appDataDir, "appsettings.json");
                if (File.Exists(exePath))
                {
                    DebugWrite($"Файл настроек обнаружен: {exePath} (загружены)");
                }
                else if (File.Exists(appDataPath))
                {
                    DebugWrite($"Файл настроек обнаружен: {appDataPath} (загружены)");
                }
                else
                {
                    DebugWrite($"Файл настроек не обнаружен: {exePath} и {appDataPath} (применяются настройки по умолчанию)");
                }

                // Работа с файлом логов
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scan_debug.log");
                if (File.Exists(logPath))
                {
                    DebugWrite($"Файл лога обнаружен: {logPath}");
                    if (Properties.Settings.Default.Debug && txtLog != null)
                    {
                        try { txtLog.Text = File.ReadAllText(logPath); txtLog.Visible = true; }
                        catch { }
                    }
                }
                else
                {
                    // пытаемся создать файл лога
                    try
                    {
                        File.WriteAllText(logPath, DateTime.Now.ToString("s") + " Лог создан" + Environment.NewLine);
                        DebugWrite($"Файл лога не обнаружен — создан: {logPath}");
                    }
                    catch (Exception ex)
                    {
                        DebugWrite($"Ошибка создания файла лога: {ex.Message}");
                    }
                }

                if (txtLog != null) txtLog.Visible = Properties.Settings.Default.Debug;
                if (btnClearLogs != null) btnClearLogs.Visible = Properties.Settings.Default.Debug;
            }
            catch { }
        }

        // Обработчик кнопки Apply — сохраняем настройки и при необходимости прячемся в трей
        private void btnApply_Click(object? sender, EventArgs e)
        {
            Properties.Settings.Default.RunInTray = chkRunInTray.Checked;
            Properties.Settings.Default.NotificationsEnabled = chkNotifications.Checked;
            Properties.Settings.Default.Debug = chkDebug.Checked;
            try { Properties.Settings.Default.Save(); DebugWrite($"Настройки сохранены: {Scan_Background.Properties.Settings.FilePath}"); }
            catch { DebugWrite("Ошибка сохранения настроек"); }

            // Видимость логов обновляем сразу
            if (txtLog != null)
            {
                txtLog.Visible = Properties.Settings.Default.Debug;
            }

            if (chkRunInTray.Checked)
            {
                HideToTray();
            }
        }

        // При закрытии формы — если настроено сворачивание в трей, прячем
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (Properties.Settings.Default.RunInTray && e.CloseReason == CloseReason.UserClosing)
            {
                // сохраняем настройки перед скрытием
                try { Properties.Settings.Default.Save(); } catch { }
                e.Cancel = true;
                HideToTray();
            }
            else
            {
                // сохраняем настройки и убираем иконку
                try { Properties.Settings.Default.Save(); DebugWrite($"Настройки сохранены при выходе: {Scan_Background.Properties.Settings.FilePath}"); } catch { DebugWrite("Ошибка сохранения настроек при выходе"); }
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        // Обработчик переключения отладки
        private void chkDebug_CheckedChanged(object? sender, EventArgs e)
        {
            Properties.Settings.Default.Debug = chkDebug.Checked;
            try { Properties.Settings.Default.Save(); } catch { }
            if (txtLog != null)
            {
                txtLog.Visible = chkDebug.Checked;
                if (chkDebug.Checked)
                {
                    // загружаем текущий файл логов
                    try
                    {
                        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scan_debug.log");
                        if (File.Exists(logPath)) txtLog.Text = File.ReadAllText(logPath);
                    }
                    catch { }
                }
            }
            if (btnClearLogs != null) btnClearLogs.Visible = chkDebug.Checked;
        }

        // Обнулить файл логов и UI
        private void btnClearLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scan_debug.log");
                File.WriteAllText(logPath, DateTime.Now.ToString("s") + " Лог очищен" + Environment.NewLine);
                if (txtLog != null) txtLog.Text = File.ReadAllText(logPath);
            }
            catch (Exception ex)
            {
                DebugWrite($"Ошибка при очистке логов: {ex.Message}");
            }
        }

        private async void runScan()
        {
            scanning = true;
            try
            {
                var napsPath = Path.Combine("C:", "Program Files", "NAPS2", "NAPS2.Console.exe");
                if (!File.Exists(napsPath))
                {
                    DebugWrite($"NAPS2 не найден по пути: {napsPath}");
                    return;
                }

                var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scans");
                // Создаём папку для сканов рядом с приложением
                try { Directory.CreateDirectory(outDir); } catch { }
                // создаём уникальное имя scan1.pdf, scan2.pdf ...
                string outFile;
                int idx = 1;
                do
                {
                    outFile = Path.Combine(outDir, $"scan{idx}.pdf");
                    idx++;
                } while (File.Exists(outFile) && idx < 100000);

                // Сначала получим список доступных устройств, чтобы указать устройство явно
                string chosenDevice = string.Empty;
                DebugWrite("Начало runScan: получение списка устройств");
                try
                {
                    var listPsi = new ProcessStartInfo
                    {
                        FileName = napsPath,
                        Arguments = "--listdevices --driver wia",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = outDir
                    };
                    try
                    {
                        using (var listProc = Process.Start(listPsi))
                        {
                            if (listProc != null)
                            {
                                var outText = await listProc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                                var errText = await listProc.StandardError.ReadToEndAsync().ConfigureAwait(false);
                                await listProc.WaitForExitAsync().ConfigureAwait(false);
                                DebugWrite($"NAPS2 --listdevices stdout: {outText}");
                                if (!string.IsNullOrWhiteSpace(errText)) DebugWrite($"NAPS2 --listdevices stderr: {errText}");
                                // Попробуем выбрать первую непустую строку как имя устройства
                                using (var sr = new StringReader(outText))
                                {
                                    string? line;
                                    while ((line = sr.ReadLine()) != null)
                                    {
                                        line = line.Trim();
                                        if (line.Length > 0)
                                        {
                                            chosenDevice = line;
                                            break;
                                        }
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(chosenDevice)) DebugWrite($"Выбрано устройство: {chosenDevice}");
                                else DebugWrite("Устройство не выбрано (список пуст)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugWrite($"Ошибка получения списка устройств: {ex.Message}");
                    }
                }
                catch { }

                // Формируем аргументы для сканирования. Добавляем --device, --driver и -v для логов.
                // Убираем --silent чтобы поведение соответствовало ручному запуску (показывает прогресс при отладке)
                var args = $"-o \"{outFile}\" -n 1 -v --driver wia --noprofile";
                if (!string.IsNullOrWhiteSpace(chosenDevice))
                {
                    // Если имя устройства содержит двоеточия/идентификаторы, используем его в кавычках
                    args += " --device \"" + chosenDevice.Replace("\"", "\'") + "\"";
                }
                DebugWrite($"Запуск NAPS2: {napsPath} {args}");

                DebugWrite("Попытка интерактивного запуска NAPS2 (показ окна)");
                bool interactiveSucceeded = false;
                try
                {
                    var psiInteractive = new ProcessStartInfo
                    {
                        FileName = napsPath,
                        Arguments = args + " --progress",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WorkingDirectory = outDir
                    };
                    try
                    {
                        var ip = Process.Start(psiInteractive);
                        if (ip != null)
                        {
                            DebugWrite($"Интерактивный процесс запущен, PID={ip.Id}");
                            var finished = await System.Threading.Tasks.Task.Run(() => ip.WaitForExit(300_000)).ConfigureAwait(false);
                            try { if (ip.HasExited) DebugWrite($"Интерактивный процесс завершился с кодом {ip.ExitCode}"); } catch { }
                            if (File.Exists(outFile))
                            {
                                DebugWrite($"Скан готов (интерактивно): {outFile}");
                                try { if (File.Exists(watchPath)) File.Delete(watchPath); DebugWrite($"Управляющий файл удалён: {watchPath}"); } catch (Exception ex) { DebugWrite($"Не удалось удалить управляющий файл: {ex.Message}"); }
                                interactiveSucceeded = true;
                            }
                            else
                            {
                                DebugWrite("Интерактивный запуск завершился, но файл не найден");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugWrite($"Ошибка интерактивного запуска NAPS2: {ex.Message}");
                    }
                }
                catch { }

                if (!interactiveSucceeded)
                {
                    DebugWrite("Попытка запуска NAPS2 напрямую");

                    var quotedExe = '"' + napsPath + '"';
                    var command = quotedExe + " " + args;
                    DebugWrite($"Запуск через cmd: {command}");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C " + '"' + command + '"',
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = outDir
                    };

                    Process? p = null;
                    try
                    {
                        p = Process.Start(psi);
                        if (p == null)
                        {
                            DebugWrite("Не удалось запустить cmd.exe (Process.Start вернул null)");
                        }
                        else
                        {
                            DebugWrite($"Процесс cmd запущен, PID={p.Id}");
                            // Небольшая пауза, затем проверим, запустился ли процесс NAPS2 напрямую как дочерний
                            try
                            {
                                await System.Threading.Tasks.Task.Delay(500).ConfigureAwait(false);
                                var exeName = Path.GetFileNameWithoutExtension(napsPath);
                                var procs = Process.GetProcessesByName(exeName);
                                if (procs.Length > 0)
                                {
                                    DebugWrite($"Обнаружено процессов {exeName}: {procs.Length}");
                                    foreach (var pr in procs)
                                    {
                                        try { DebugWrite($"Найдён {exeName} PID={pr.Id} (StartTime={pr.StartTime})"); } catch { DebugWrite($"Найдён {exeName} PID={pr.Id}"); }
                                    }
                                    // Подождём окончания первого найденного процесса (в фоне)
                                    try
                                    {
                                        var target = procs[0];
                                        DebugWrite($"Ожидание завершения процесса {target.Id}");
                                        var finished2 = await System.Threading.Tasks.Task.WhenAny(target.WaitForExitAsync(), System.Threading.Tasks.Task.Delay(300_000)).ConfigureAwait(false);
                                        if (finished2 == target.WaitForExitAsync())
                                        {
                                            try { DebugWrite($"Процесс {target.Id} завершился с кодом {target.ExitCode}"); } catch { DebugWrite($"Процесс {target.Id} завершился"); }
                                        }
                                        else
                                        {
                                            DebugWrite($"Процесс {target.Id} не завершился за 5 минут");
                                        }
                                    }
                                    catch { }
                                }
                                else
                                {
                                    DebugWrite($"Процесс {exeName} не обнаружен среди запущенных процессов");
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugWrite($"Ошибка проверки дочерних процессов: {ex.Message}");
                            }
                            var timeoutMs = 300_000; // 5 минут
                            var exitTask = p.WaitForExitAsync();
                            var finished = await System.Threading.Tasks.Task.WhenAny(exitTask, System.Threading.Tasks.Task.Delay(timeoutMs)).ConfigureAwait(false);
                            string output = string.Empty;
                            string error = string.Empty;
                            try
                            {
                                output = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                                error = await p.StandardError.ReadToEndAsync().ConfigureAwait(false);
                            }
                            catch { }

                            if (finished == exitTask)
                            {
                                DebugWrite($"cmd завершился, вывод: {output}");
                                if (!string.IsNullOrWhiteSpace(error)) DebugWrite($"cmd stderr: {error}");
                            }
                            else
                            {
                                DebugWrite("cmd не завершился в отведённое время, пытаюсь завершить процесс");
                                try { p.Kill(true); } catch { }
                                if (!string.IsNullOrWhiteSpace(output)) DebugWrite($"cmd stdout (partial): {output}");
                                if (!string.IsNullOrWhiteSpace(error)) DebugWrite($"cmd stderr (partial): {error}");
                            }

                            if (File.Exists(outFile))
                            {
                                DebugWrite($"Скан готов: {outFile}");
                                try { if (File.Exists(watchPath)) File.Delete(watchPath); DebugWrite($"Управляющий файл удалён: {watchPath}"); } catch (Exception ex) { DebugWrite($"Не удалось удалить управляющий файл: {ex.Message}"); }
                            }
                            else
                            {
                                DebugWrite($"Файл скана не найден после cmd-запуска: {outFile}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugWrite($"Ошибка запуска cmd: {ex.Message}");
                    }
                }
            }
            finally
            {
                scanning = false;
            }
        }


    }
}
