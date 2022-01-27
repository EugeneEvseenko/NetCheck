using NetCheck.Properties;
using Microsoft.Win32;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Media;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Net.NetworkInformation;
using System.Windows.Shell;

namespace NetCheck
{
    public partial class MainWindow : Window
    {
        string localAppDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int cmdShow);
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr handle);
        readonly Mutex mutex = new Mutex(false, "FA95CA09-69DE-4C93-92AC-8518FACD113D");
        public class MemoryManagement
        {
            [DllImport("kernel32.dll")]
            public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

            public void FlushMemory()
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
        }

        public void SetAutorun(bool mode)
        {
            try
            {
                reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
                reg.DeleteValue("NetCheck", false);
                if (mode)
                {
                    reg.SetValue("NetCheck", System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                reg.Close();
            }
            catch
            {
                MessageBox.Show("Произошла ошибка при добавлении программы в автозагрузку", "Ошибочка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public MainWindow()
        {
            if (!mutex.WaitOne(500, false))
            {
                MessageBox.Show("Приложение уже запущено!", "Ошибочка!");
                string processName = Process.GetCurrentProcess().ProcessName;
                Process process = Process.GetProcesses().Where(p => p.ProcessName == processName).FirstOrDefault();
                if (process != null)
                {
                    IntPtr handle = process.MainWindowHandle;
                    ShowWindow(handle, 1);
                    SetForegroundWindow(handle);
                }
                Close();
                return;
            }
            InitializeComponent();
        }
        //[Guid("FA95CA09-69DE-4C93-92AC-8518FACD113D")]
        RegistryKey reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
        private MemoryManagement MM = new MemoryManagement();
        TaskbarIcon tbi;
        ContextMenu menu = new ContextMenu();
        MenuItem exitItem = new MenuItem { Header = "Выход" };
        SoundPlayer soundPlayer = new SoundPlayer();
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        Dialog dialog;
        ObservableCollection<ServerItem> Servers = new ObservableCollection<ServerItem>();
        TaskbarItemInfo taskbar = new TaskbarItemInfo();
        bool firstStart = true;
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TaskbarItemInfo = taskbar;
            serversList.ItemsSource = Servers;
            foreach (string item in Settings.Default.ServersList)
            {
                Servers.Add(new ServerItem
                {
                    Title = item,
                    Description = "Ожидание проверки"
                });
            }
            serversList.Items.Refresh();
            ResizeMode = Settings.Default.hideWhenClose ? ResizeMode.NoResize : ResizeMode.CanMinimize;
            Settings.Default.PropertyChanged += Default_PropertyChanged;
            /* Exit Entry Context Menu */
            exitItem.Click += ExitItem_Click;
            menu.Items.Add(exitItem);
            /* Taskbar */
            tbi = new TaskbarIcon();
            tbi.TrayLeftMouseDown += OnClickTrayIcon;
            tbi.Icon = Properties.Resources.tray;
            tbi.ContextMenu = menu;
            /* Load Settings */
            periodCB.SelectedIndex = Settings.Default.updTime;
            SoundLostCB.IsChecked = Settings.Default.doSoundLost;
            SoundShowCB.IsChecked = Settings.Default.doSoundShow;
            PushLostCB.IsChecked = Settings.Default.doPushLost;
            PushShowCB.IsChecked = Settings.Default.doPushShow;
            HideWhenCloseCB.IsChecked = Settings.Default.hideWhenClose;
            turboCB.IsChecked = Settings.Default.TurboCheck;
            autorunCheckbox.IsChecked = reg.GetValue("NetCheck", null) != null;
            OnTime();
            timer.Tick += Timer_Tick;
            timer.Interval = Helper.GetPeriod(periodCB.SelectedIndex);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (progressUpdate.IsIndeterminate) { return; } else { OnTime(); }
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Console.WriteLine(e.PropertyName);
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop(); 
            Application.Current.Shutdown();
        }

        private void OnClickTrayIcon(object sender, RoutedEventArgs e)
        {
            Show();
            Focus();
        }
        
        public async void OnTime()
        {
            try
            {
                if (!CheckList()) return;
                taskbar.ProgressState = TaskbarItemProgressState.Indeterminate;
                progressUpdate.IsIndeterminate = true;
                foreach (string server in Settings.Default.ServersList)
                {
                    int tempServerIndex = Servers.IndexOf(Servers.First(x => x.Title == server));
                    Servers[tempServerIndex].ConnectionBrush = new SolidColorBrush(Colors.Orange);
                    Servers[tempServerIndex].Description = "Проверка..."; serversList.Items.Refresh();
                    StatusLabel.Content = string.Format("Обновление... [ {0} ]", server);
                    PingResponse TempState = await Helper.CheckConnection(server);
                    if (Servers[tempServerIndex].IsOnline != (TempState.Status == IPStatus.Success))
                    {
                        Servers[tempServerIndex].IsOnline = TempState.Status == IPStatus.Success;
                        if (!firstStart)
                        {
                            if ((TempState.Status == IPStatus.Success) && (bool)SoundShowCB.IsChecked)
                            {
                                soundPlayer.Stream = Properties.Resources.sound_1;
                                soundPlayer.Play();
                            }
                            if (!(TempState.Status == IPStatus.Success) && (bool)SoundLostCB.IsChecked)
                            {
                                soundPlayer.Stream = Properties.Resources.sound_2;
                                soundPlayer.Play();
                            }
                            if ((TempState.Status == IPStatus.Success) && (bool)PushShowCB.IsChecked)
                            {
                                new Notification(NotificationType.Fine, "Связь с " + server + " есть").Show();
                            }
                            if (!(TempState.Status == IPStatus.Success) && (bool)PushLostCB.IsChecked)
                            {
                                new Notification(NotificationType.Lost, "Связь с " + server + " отсутствует").Show();
                            }
                        }

                    }
                    Servers[tempServerIndex].ConnectionBrush = new SolidColorBrush((TempState.Status == IPStatus.Success) ? Colors.Green : Colors.Red);
                    Servers[tempServerIndex].Description = (TempState.Status == IPStatus.Success) ? "Доступен (" + TempState.PingTime.ToString() + "мс)" : "Недоступен"; serversList.Items.Refresh();
                }
                if (firstStart) firstStart = false;
                StatusLabel.Content = "Ожидание проверки";
                timer.Start();
                progressUpdate.IsIndeterminate = false;
                taskbar.ProgressState = TaskbarItemProgressState.None;
            }
            catch(Exception ex)
            {
                Console.WriteLine("============== OnTime ==============");
                Console.WriteLine(ex.Message);
                Console.WriteLine("====================================");
                progressUpdate.IsIndeterminate = false;
                taskbar.ProgressState = TaskbarItemProgressState.None;
            }
            MM.FlushMemory();
        }
        private void AutorunCheckbox_Click(object sender, RoutedEventArgs e)
        {
            SetAutorun((bool)autorunCheckbox.IsChecked);
        }

        private void PeriodCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.updTime = (byte)periodCB.SelectedIndex;
            Settings.Default.Save();
            timer.Stop();
            timer.Interval = Helper.GetPeriod(periodCB.SelectedIndex);
            timer.Start();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Settings.Default.hideWhenClose)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                timer.Stop();
                Application.Current.Shutdown();
            }
        }

        private void SoundLostCB_Check(object sender, RoutedEventArgs e)
        {
            Settings.Default.doSoundLost = (bool)SoundLostCB.IsChecked;
            Settings.Default.Save();
        }

        private void SoundShowCB_Check(object sender, RoutedEventArgs e)
        {
            Settings.Default.doSoundShow = (bool)SoundShowCB.IsChecked;
            Settings.Default.Save();
        }

        private void PushLostCB_Check(object sender, RoutedEventArgs e)
        {
            Settings.Default.doPushLost = (bool)PushLostCB.IsChecked;
            Settings.Default.Save();
        }

        private void PushShowCB_Check(object sender, RoutedEventArgs e)
        {
            Settings.Default.doPushShow = (bool)PushShowCB.IsChecked;
            Settings.Default.Save();
        }

        private void HideWhenCloseCB_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.hideWhenClose = (bool)HideWhenCloseCB.IsChecked;
            Settings.Default.Save();
            ResizeMode = Settings.Default.hideWhenClose ? ResizeMode.NoResize : ResizeMode.CanMinimize;
        }

        private void AddServerBtn_Click(object sender, RoutedEventArgs e)
        {
            dialog = new Dialog();
            if ((bool)dialog.ShowDialog())
            {
                string server = dialog.adressTb.Text.Trim();
                if(Servers.Count(s => s.Title.Equals(server, StringComparison.OrdinalIgnoreCase)) > 0)
                {
                    MessageBox.Show("Такой сервер уже есть!", "Ой-ёй!", MessageBoxButton.OK, MessageBoxImage.Information); return;
                }
                Settings.Default.ServersList.Add(server);
                Settings.Default.Save();
                Servers.Add(new ServerItem
                {
                    Title = dialog.adressTb.Text,
                    Description = "Ожидание проверки"
                });
                if (!progressUpdate.IsIndeterminate) OnTime();
            }
        }

        private void serversList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveServerBtn.IsEnabled = serversList.SelectedIndex >= 0;
        }

        private void RemoveServerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить " + Servers[serversList.SelectedIndex].Title, 
                "Подтвердите удаление", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Settings.Default.ServersList.Remove(Servers[serversList.SelectedIndex].Title);
                Settings.Default.Save();
                Servers.RemoveAt(serversList.SelectedIndex);
                CheckList();
            }
        }
        public bool CheckList()
        {
            if (serversList.Items.Count == 0)
            {
                StatusLabel.Content = "Давай добавим сервера для проверки?";
                timer.Start();
                return false;
            }
            return true;
        }
        private void turboCB_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.TurboCheck = (bool)turboCB.IsChecked;
            Settings.Default.Save();
        }
    }
}
