using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetCheck
{
    /// <summary>
    /// Логика взаимодействия для Notification.xaml
    /// </summary>
    public partial class Notification : Window
    {
        DoubleAnimation openAnimation, closeAnimation;
        Rect primaryMonitorArea = SystemParameters.WorkArea;
        System.Windows.Forms.Timer CloseTimer = new System.Windows.Forms.Timer();
        SolidColorBrush successColor = new SolidColorBrush(Color.FromRgb(0, 132, 255));
        SolidColorBrush dangerColor = new SolidColorBrush(Color.FromRgb(255, 41, 41));
        public Notification(NotificationType notificationType, string title)
        {
            InitializeComponent();
            BackRect.Fill = notificationType == NotificationType.Fine ? successColor : dangerColor;
            TitleBlock.Text = title;
            double from = primaryMonitorArea.Right - Width + 400;
            double to = primaryMonitorArea.Right - Width - 25;
            Top = primaryMonitorArea.Bottom - Height - 50;
            openAnimation = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(400)))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            closeAnimation = new DoubleAnimation(to, from, new Duration(TimeSpan.FromMilliseconds(400)))
            {
                EasingFunction = new CircleEase()
            };
            closeAnimation.Completed += CloseAnimation_Completed;
            openAnimation.Completed += (s, a) =>
            {
                CloseTimer = new System.Windows.Forms.Timer
                {
                    Interval = 5000
                };
                CloseTimer.Tick += CloseTimer_Elapsed;
                CloseTimer.Start();
            };
        }

        private void CloseAnimation_Completed(object sender, EventArgs e)
        {
            Close();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseTimer.Stop();
            Opacity = 1;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseTimer.Start();
            Opacity = 0.85;
        }

        private void CloseTimer_Elapsed(object sender, EventArgs e)
        {
            CloseTimer.Stop();
            CloseTimer.Dispose();
            BeginAnimation(LeftProperty, closeAnimation);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BeginAnimation(LeftProperty, closeAnimation);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAnimation(LeftProperty, openAnimation);
        }

    }
}
