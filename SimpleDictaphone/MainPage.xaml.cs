using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Phone.Shell;

namespace SimpleDictaphone
{
    public partial class MainPage : PhoneApplicationPage
    {
        private readonly AudioManager _audioManager;
        private DispatcherTimer _xnaTimer;
        private DispatcherTimer _recordTimer;
        private DateTime _recordStartTime;
        private bool _isRecording;


        public MainPage()
        {
            InitializeComponent();
            _audioManager = new AudioManager();
            _xnaTimer = new DispatcherTimer();
            _xnaTimer.Interval = TimeSpan.FromMilliseconds(50);
            _xnaTimer.Tick += (s, e) => FrameworkDispatcher.Update();
            _xnaTimer.Start();
            _recordTimer = new DispatcherTimer();
            _recordTimer.Interval = TimeSpan.FromSeconds(1);
            _recordTimer.Tick += RecordTimer_Tick;
            BuildApplicationBar();
            TimerText.Text = "00:00";
            _isRecording = false;

            LoadRecordings();
        }

        private void SetIdleDetection(bool enabled)
        {
            try
            {
                PhoneApplicationService.Current.ApplicationIdleDetectionMode =
                    enabled ? IdleDetectionMode.Enabled : IdleDetectionMode.Disabled;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void LoadRecordings()
        {
            var files = _audioManager.GetAllRecordings();
            
        }

        private void RecordTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _recordStartTime;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            TimerText.Text = string.Format("{0:00}:{1:00}",
                (int)elapsed.TotalMinutes,
                elapsed.Seconds);
        }

        private void RecordToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                SetIdleDetection(false);
                string name = _audioManager.StartRecording();
                _recordStartTime = DateTime.Now;
                _recordTimer.Start();
                BlinkingAnimation.Begin();
                _isRecording = true;
            }
            else
            {
                _audioManager.StopRecording();
                _recordTimer.Stop();
                BlinkingAnimation.Stop();
                RecordDot.Opacity = 1.0;
                _isRecording = false;
                SetIdleDetection(true);
                TimerText.Text = "00:00";
                LoadRecordings();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRecordings();
        }

        private void OpenRecordingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/RecordingsPage.xaml", UriKind.Relative));
        }

        private void RecordingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // абвгдежзи
        }

        private void AboutAppMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutAppPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_isRecording)
            {
                _audioManager.StopRecording();
                _isRecording = false;
                SetIdleDetection(true);
            }
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            ApplicationBarMenuItem menuSettings = new ApplicationBarMenuItem();
            menuSettings.Text = SimpleDictaphone.Resources.AppResources.AppBarAbout;
            menuSettings.Click += AboutAppMenuItem_Click;
            ApplicationBar.MenuItems.Add(menuSettings);

            ApplicationBarIconButton buttonSearch = new ApplicationBarIconButton();
            buttonSearch.IconUri = new Uri("/Assets/AppBar/list.png", UriKind.Relative);
            buttonSearch.Text = SimpleDictaphone.Resources.AppResources.AppBarList;
            buttonSearch.Click += OpenRecordingsButton_Click;
            ApplicationBar.Buttons.Add(buttonSearch);
        }
    }
}
