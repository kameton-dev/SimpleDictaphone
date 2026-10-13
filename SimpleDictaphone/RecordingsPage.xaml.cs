using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Storage;
using AppResources = SimpleDictaphone.Resources.AppResources;
using System.Windows.Media;

namespace SimpleDictaphone
{
    public partial class RecordingsPage : PhoneApplicationPage
    {
        private const string EmptyTime = "00:00 / 00:00";
        private const string MusicSubfolder = "SimpleDictaphone";

        private const string CaptionOk = "( ˶ˆᗜˆ˵ )";
        private const string CaptionSaved = "(˶ᵔ ᵕ ᵔ˶)";
        private const string CaptionNotFound = "(꒪▿꒪)";
        private const string CaptionError = "┐(‘～`;)┌";

        private static readonly char[] InvalidNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        private readonly DispatcherTimer _progressTimer;
        private string _currentPlayingFile;
        private bool _isPlaying;
        private RecordingInfo _selectedRecording;

        public RecordingsPage()
        {
            InitializeComponent();

            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;
            Player.MediaOpened += Player_MediaOpened;

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _progressTimer.Tick += ProgressTimer_Tick;

            BuildApplicationBar();
            UpdateAppBarButtonsState();
            LoadRecordings();
        }

        #region Диалоги (единая точка для всех текстов)

        private static void ShowInfo(string message, string caption = null)
        {
            MessageBox.Show(message, caption ?? string.Empty, MessageBoxButton.OK);
        }

        private static void ShowError(string messageFormat, string details)
        {
            MessageBox.Show(string.Format(messageFormat, details), CaptionError, MessageBoxButton.OK);
        }

        private static void ShowSelectRecording()
        {
            MessageBox.Show(AppResources.RecordingSelectFirst);
        }

        #endregion

        #region Загрузка списка

        private void LoadRecordings()
        {
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var groups = iso.GetFileNames("*.wav")
                    .Select(f => new RecordingInfo(f, iso.GetLastWriteTime(f).DateTime))
                    .OrderByDescending(r => r.CreatedDate)
                    .GroupBy(r => GetDateGroupName(r.CreatedDate))
                    .Select(g => new RecordingsPageGroup(g.Key, g.ToList()));

                RecordingsSelector.ItemsSource = new ObservableCollection<RecordingsPageGroup>(groups);
            }
        }

        private static string GetDateGroupName(DateTime dt)
        {
            if (dt.Date == DateTime.Today) return AppResources.RecordingsDateToday;
            if (dt.Date == DateTime.Today.AddDays(-1)) return AppResources.RecordingsDateYesterday;
            return dt.ToString("d", CultureInfo.CurrentCulture);
        }

        #endregion

        #region Плеер

        private void ResetProgressUi()
        {
            ProgressBar.Value = 0;
            TimeLabel.Text = EmptyTime;
        }

        private void ReleasePlayer()
        {
            _progressTimer.Stop();
            Player.Stop();
            Player.Source = null;
            _currentPlayingFile = null;
            _isPlaying = false;
            ResetProgressUi();
        }

        private void RecordingsSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var recording = RecordingsSelector.SelectedItem as RecordingInfo;
            if (recording == null) return;

            _selectedRecording = recording;

            if (_currentPlayingFile != recording.FileName)
                ReleasePlayer();

            UpdateAppBarButtonsState();
        }

        private void UpdateAppBarButtonsState()
        {
            bool hasSelection = _selectedRecording != null;

            SetEnabled(ApplicationBar.Buttons[0], hasSelection);                  // rewind
            SetEnabled(ApplicationBar.Buttons[1], hasSelection && !_isPlaying);   // play
            SetEnabled(ApplicationBar.Buttons[2], hasSelection && _isPlaying);    // pause
            SetEnabled(ApplicationBar.Buttons[3], hasSelection);                  // save

            SetEnabled(ApplicationBar.MenuItems[0], hasSelection);                // rename
            SetEnabled(ApplicationBar.MenuItems[1], hasSelection);                // delete
        }

        private static void SetEnabled(object item, bool enabled)
        {
            var button = item as IApplicationBarIconButton;
            if (button != null) { button.IsEnabled = enabled; return; }

            var menuItem = item as IApplicationBarMenuItem;
            if (menuItem != null) menuItem.IsEnabled = enabled;
        }

        #endregion

        #region Воспроизведение

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null) { ShowSelectRecording(); return; }

            if (Player.Source != null && _currentPlayingFile == _selectedRecording.FileName)
            {
                StartPlayback();
                return;
            }

            try
            {
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!iso.FileExists(_selectedRecording.FileName))
                    {
                        ShowInfo(AppResources.RecordingFileNotFound, CaptionNotFound);
                        LoadRecordings();
                        return;
                    }

                    var stream = iso.OpenFile(_selectedRecording.FileName, FileMode.Open, FileAccess.Read);
                    _currentPlayingFile = _selectedRecording.FileName;
                    Player.SetSource(stream);
                }
            }
            catch (Exception ex)
            {
                ShowError(AppResources.RecordingPlaybackError, ex.Message);
            }
        }

        private void StartPlayback()
        {
            Player.Play();
            _isPlaying = true;
            _progressTimer.Start();
            UpdateAppBarButtonsState();
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            StartPlayback();
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (Player.CurrentState == MediaElementState.Playing)
            {
                Player.Pause();
                _progressTimer.Stop();
                _isPlaying = false;
                UpdateAppBarButtonsState();
            }
            else if (Player.CurrentState == MediaElementState.Paused)
            {
                StartPlayback();
            }
        }

        private void RewindButton_Click(object sender, EventArgs e)
        {
            Player.Position = TimeSpan.Zero;
            ProgressBar.Value = 0;

            string total = Player.NaturalDuration.HasTimeSpan
                ? string.Format("{0:mm\\:ss}", Player.NaturalDuration.TimeSpan)
                : "00:00";
            TimeLabel.Text = "00:00 / " + total;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (!Player.NaturalDuration.HasTimeSpan) return;

            var total = Player.NaturalDuration.TimeSpan;
            var current = Player.Position;

            ProgressBar.Maximum = 100;
            ProgressBar.Value = current.TotalSeconds / total.TotalSeconds * 100;
            TimeLabel.Text = string.Format("{0:mm\\:ss} / {1:mm\\:ss}", current, total);
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _progressTimer.Stop();
            _isPlaying = false;
            ResetProgressUi();
            UpdateAppBarButtonsState();
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            string details = e.ErrorException != null
                ? e.ErrorException.Message
                : AppResources.RecordingUnknownError;

            ShowError(AppResources.RecordingPlaybackError, details);

            _progressTimer.Stop();
            _isPlaying = false;
            UpdateAppBarButtonsState();
        }

        #endregion

        #region Удаление

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null) return;

            if (MessageBox.Show(AppResources.RecordingDeleteConfirm, AppResources.RecordingDeleteTitle,
                    MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;

            try
            {
                ReleasePlayer();

                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (iso.FileExists(_selectedRecording.FileName))
                        iso.DeleteFile(_selectedRecording.FileName);
                }

                _selectedRecording = null;
                LoadRecordings();
                UpdateAppBarButtonsState();
            }
            catch (Exception ex)
            {
                ShowError(AppResources.RecordingDeleteError, ex.Message);
            }
        }

        #endregion

        #region Переименование

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null) { ShowSelectRecording(); return; }

            ShowRenameDialog(_selectedRecording.DisplayName);
        }

        private void ShowRenameDialog(string currentName)
        {
            var textBox = new TextBox
            {
                Text = currentName,
                AcceptsReturn = false,
                TextWrapping = TextWrapping.Wrap
            };
            textBox.GotFocus += (s, e) => textBox.SelectAll();

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = AppResources.RecordingRenameWarning,
                Margin = new Thickness(10, 10, 10, 8)
            });
            panel.Children.Add(textBox);

            var dialog = new CustomMessageBox
            {
                Title = AppResources.RecordingRenameTitle,
                Content = panel,
                LeftButtonContent = AppResources.ButtonOK,
                RightButtonContent = AppResources.ButtonCancel,
                IsFullScreen = false
            };

            dialog.Dismissed += (s, e) =>
            {
                if (e.Result != CustomMessageBoxResult.LeftButton) return;

                string newName = textBox.Text;

                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show(AppResources.RecordingNameEmpty);
                    return;
                }

                if (newName.IndexOfAny(InvalidNameChars) >= 0)
                {
                    MessageBox.Show(AppResources.RecordingNameInvalid);
                    return;
                }

                PerformRename(newName);
            };

            dialog.Show();
        }

        private void PerformRename(string newName)
        {
            try
            {
                ReleasePlayer();

                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string oldFileName = _selectedRecording.FileName;

                    if (!iso.FileExists(oldFileName))
                    {
                        ShowInfo(AppResources.RecordingFileNotFound, CaptionNotFound);
                        return;
                    }

                    string newFileName = Path.GetFileNameWithoutExtension(oldFileName) + "_" + newName + ".wav";

                    if (iso.FileExists(newFileName))
                        iso.DeleteFile(newFileName);

                    iso.MoveFile(oldFileName, newFileName);
                }

                _selectedRecording = null;
                LoadRecordings();
                UpdateAppBarButtonsState();
                ShowInfo(AppResources.RecordingRenamed, CaptionOk);
            }
            catch (Exception ex)
            {
                ShowError(AppResources.RecordingRenameError, ex.Message);
            }
        }

        #endregion

        #region Сохранение в Music

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null) return;

            try
            {
                byte[] buffer;
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!iso.FileExists(_selectedRecording.FileName))
                    {
                        ShowInfo(AppResources.RecordingFileNotFound, CaptionNotFound);
                        return;
                    }

                    using (var fileStream = iso.OpenFile(_selectedRecording.FileName, FileMode.Open, FileAccess.Read))
                    using (var ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        buffer = ms.ToArray();
                    }
                }

                var appFolder = await KnownFolders.MusicLibrary.CreateFolderAsync(
                    MusicSubfolder, CreationCollisionOption.OpenIfExists);

                var file = await appFolder.CreateFileAsync(
                    _selectedRecording.FileName, CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteBytesAsync(file, buffer);

                ShowInfo(AppResources.RecordingSaved, CaptionSaved);
            }
            catch (Exception ex)
            {
                ShowError(AppResources.RecordingSaveError, ex.Message);
            }
        }

        #endregion

        #region Локализованный AppBar (сейчас не вызывается — см. комментарий)

        // Порядок MenuItems ДОЛЖЕН совпадать с UpdateAppBarButtonsState: [0] = rename, [1] = delete.
        // Чтобы использовать вместо XAML-панели: уберите <ApplicationBar> из XAML и вызовите
        // BuildApplicationBar() в конструкторе перед UpdateAppBarButtonsState().
        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            AddMenuItem(AppResources.AppBarRename, RenameMenuItem_Click);
            AddMenuItem(AppResources.AppBarDelete, DeleteMenuItem_Click);

            AddButton("rew", AppResources.AppBarRewind, RewindButton_Click);
            AddButton("play", AppResources.AppBarPlay, PlayButton_Click);
            AddButton("pause", AppResources.AppBarPause, PauseButton_Click);
            AddButton("download", AppResources.AppBarSave, SaveButton_Click, "/Assets/AppBar/download.png");
        }

        private void AddMenuItem(string text, EventHandler handler)
        {
            var item = new ApplicationBarMenuItem(text);
            item.Click += handler;
            ApplicationBar.MenuItems.Add(item);
        }

        private void AddButton(string icon, string text, EventHandler handler, string iconPath = null)
        {
            var button = new ApplicationBarIconButton
            {
                IconUri = new Uri(iconPath ?? "/Assets/AppBar/transport." + icon + ".png", UriKind.Relative),
                Text = text
            };
            button.Click += handler;
            ApplicationBar.Buttons.Add(button);
        }

        #endregion
    }
}