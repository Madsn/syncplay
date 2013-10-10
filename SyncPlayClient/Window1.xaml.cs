using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Net;
using System.Timers;

namespace SyncPlayer
{
    public partial class Window1 : Window
    {
        private static System.Timers.Timer aTimer;
        DispatcherTimer timer;
        bool fullScreen = false;
        double currentposition = 0;
        private string syncPartnerName;
        private string syncName = System.Environment.MachineName + "1";
        private bool syncing;

        #region Constructor
        public Window1()
        {
            InitializeComponent();
            IsPlaying(false);
            syncNameLabel.Text = syncName;
            syncPartnerName = syncPartnerNameTextbox.Text;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += new EventHandler(timer_Tick);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:8080/SyncPlayServer/hello/create?name=" + this.syncName
                + "&seconds=" + this.MediaEL.Position.TotalSeconds + "&movie=x" + this.MediaEL.Source);
            request.GetResponse();

            aTimer = new System.Timers.Timer(2000);

            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Enabled = false;
        }
        #endregion

        #region IsPlaying(bool)
        private void IsPlaying(bool bValue)
        {
            btnStop.IsEnabled = bValue;
            btnMoveBackward.IsEnabled = bValue;
            btnMoveForward.IsEnabled = bValue;
            btnPlay.IsEnabled = bValue;
            syncWithPartner.IsEnabled = bValue;
            seekBar.IsEnabled = bValue;
        } 
        #endregion

        #region Play and Pause
        private void btnPlay_Click()
        {
            IsPlaying(true);
            if (btnPlay.Content.ToString() == "Play")
            {
                MediaEL.Play();
                btnPlay.Content = "Pause";
            }
            else
            {
                MediaEL.Pause();
                btnPlay.Content = "Play";
            }
        }
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            btnPlay_Click();
        } 
        #endregion

        #region Stop
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            MediaEL.Stop();
            btnPlay.Content = "Play";
            IsPlaying(false);
            btnPlay.IsEnabled = true;
        } 
        #endregion

        #region Back and Forward
        private void btnMoveForward_Click(object sender, RoutedEventArgs e)
        {
            MediaEL.Position = MediaEL.Position + TimeSpan.FromSeconds(10);
        }

        private void btnMoveBackward_Click(object sender, RoutedEventArgs e)
        {
            MediaEL.Position = MediaEL.Position - TimeSpan.FromSeconds(10);
        } 
        #endregion

        #region Open Media
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Video Files (*.*)|*.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MediaEL.Source = new Uri(ofd.FileName);
                btnPlay.IsEnabled = true;
            }
        }
        #endregion

        #region Sync with partner
        private void syncWithPartner_Click(object sender, RoutedEventArgs e)
        {
            if (!syncing)
                return;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:8080/SyncPlayServer/hello/get?name=" + syncPartnerName);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                //Console.WriteLine(reader.ReadToEnd());
                double seconds = double.Parse(reader.ReadToEnd());
                MediaEL.Position = System.TimeSpan.FromSeconds(seconds);
            }
            response.Close();
        } 
        #endregion

        #region Seek Bar
        private void MediaEL_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (MediaEL.NaturalDuration.HasTimeSpan)
            {
                TimeSpan ts = MediaEL.NaturalDuration.TimeSpan;
                seekBar.Maximum = ts.TotalSeconds;
                seekBar.SmallChange = 1;
                seekBar.LargeChange = Math.Min(10, ts.Seconds / 10);
            }
            timer.Start();
        }

        bool isDragging = false;

        void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                seekBar.Value = MediaEL.Position.TotalSeconds;
                currentposition = seekBar.Value;
            }
        } 

        private void seekBar_DragStarted(object sender, DragStartedEventArgs e)
        {
            isDragging = true;
        }

        private void seekBar_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            MediaEL.Position = TimeSpan.FromSeconds(seekBar.Value);
        }
        #endregion

        #region FullScreen
        [DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();

        System.Timers.Timer timeClick = new System.Timers.Timer((int)GetDoubleClickTime())
        {
            AutoReset = false
        };


        private void MediaEL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!timeClick.Enabled)
            {
                timeClick.Enabled = true;
                return;
            }

            if (timeClick.Enabled)
            {
                if (!fullScreen)
                {
                    LayoutRoot.Children.Remove(MediaEL);
                    this.Background = new SolidColorBrush(Colors.Black);
                    this.Content = MediaEL;
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    MediaEL.Position = TimeSpan.FromSeconds(currentposition);
                }
                else
                {
                    this.Content = LayoutRoot;
                    LayoutRoot.Children.Add(MediaEL);
                    this.Background = new SolidColorBrush(Colors.White);
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                    MediaEL.Position = TimeSpan.FromSeconds(currentposition);
                }
                fullScreen = !fullScreen;
            }
        }
        #endregion

        #region PauseWithSpaceBar
        private void MediaEL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                btnPlay_Click();
            }
        }
        #endregion

        #region Extension Methods
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:8080/SyncPlayServer/hello/update?name=" + this.syncName
                + "&seconds=" + this.MediaEL.Position.TotalSeconds + "&movie=x" + this.MediaEL.Source);
            request.GetResponse();
            }));
        }
        #endregion

        private void SyncNameLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(syncNameLabel.Text);
        }

        private void syncPartnerNameTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                syncPartnerName = syncPartnerNameTextbox.Text;
                if (syncPartnerName.Length > 0)
                {
                    syncing = true;
                    aTimer.Enabled = true;
                }
                else
                {
                    syncing = false;
                    aTimer.Enabled = false;
                }
            }
        }
    }
}
