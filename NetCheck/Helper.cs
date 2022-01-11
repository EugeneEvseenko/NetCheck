using NetCheck.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NetCheck
{
    public class ServerItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsOnline { get; set; } = false;
        public SolidColorBrush TitleBrush { get; set; } = new SolidColorBrush(Colors.Black);
        public SolidColorBrush ConnectionBrush { get; set; } = new SolidColorBrush(Colors.Black);
    }
    public class PingResponse
    {
        public IPStatus Status { get; set; }
        public long PingTime { get; set; }
    }
    public enum NotificationType
    {
        Fine, Lost
    }
    public static class Helper
    {
        public static DoubleAnimation textShow = new DoubleAnimation(25.0, new TimeSpan(0, 0, 0, 0, 300))
        {
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
        };
        public static DoubleAnimation textHide = new DoubleAnimation(0.0, new TimeSpan(0, 0, 0, 0, 300))
        {
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
        };
        public static int GetPeriod(int index)
        {
            switch (index)
            {
                case 1: return 5 * 1000;
                case 2: return 10 * 1000;
                case 3: return 30 * 1000;
                case 4: return 60 * 1000;
                case 5: return 60 * 1000 * 5;
                case 6: return 60 * 1000 * 10;
                case 7: return 60 * 1000 * 30;
                case 8: return 60 * 1000 * 60;
                default: return 1000;
            }
        }
        public static Task CreateDelay(int millis)
        {
            try
            {
                return Task.Run(() =>
                {
                    Thread.Sleep(millis);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Task.CompletedTask;
            }
        }
        
        public static async Task<PingResponse> CheckConnection(string server)
        {
            PingResponse response = new PingResponse { PingTime = 0, Status = IPStatus.TimedOut };
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = await new Ping().SendPingAsync(server, Settings.Default.TurboCheck ? 300 : 2000);
                response.PingTime = pingReply.RoundtripTime;
                response.Status = pingReply.Status;
                return response;
            }
            catch(Exception ex)
            {
                Console.WriteLine("============== CheckConnection ==============");
                Console.WriteLine(ex.Message);
                Console.WriteLine("=============================================");
                return response;
            }

        }
    }
}
