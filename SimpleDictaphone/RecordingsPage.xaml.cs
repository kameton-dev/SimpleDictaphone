using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace SimpleDictaphone
{
    public partial class RecordingsPage : PhoneApplicationPage
    {
        private DispatcherTimer _progressTimer;
        private string _currentPlayingFile;
        private bool _isPlaying;   // состояние проигрывания
        private RecordingInfo _selectedRecording;
        private ObservableCollection<RecordingsPageGroup> _groupedRecordings;
        private TextBlock _selectedTextBlock;

        public RecordingsPage()
        {
            InitializeComponent();

            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;
            Player.MediaOpened += Player_MediaOpened;

            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            _progressTimer.Tick += ProgressTimer_Tick;

            _isPlaying = false;
            _groupedRecordings = new ObservableCollection<RecordingsPageGroup>();
            UpdateAppBarButtonsState();

            LoadRecordings();
        }

        private void LoadRecordings()
        {
            // Вместо _audioManager используем прямое обращение к IsolatedStorage
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var allFiles = iso.GetFileNames("*.wav"); // Получаем все wav файлы
                var list = new List<RecordingInfo>();

                foreach (var file in allFiles)
                {
                    // Метод возвращает DateTimeOffset, берем из него .DateTime
                    DateTime dt = iso.GetLastWriteTime(file).DateTime;
                    list.Add(new RecordingInfo(file, dt));
                }

                // Группировка остается прежней...
                var grouped = list.OrderByDescending(r => r.CreatedDate)
                    .GroupBy(r => GetDateGroupName(r.CreatedDate))
                    .Select(g => new RecordingsPageGroup(g.Key, g.ToList()))
                    .ToList();

                _groupedRecordings = new ObservableCollection<RecordingsPageGroup>(grouped);
                RecordingsSelector.ItemsSource = _groupedRecordings;
            }
        }

        // Вспомогательный метод для красивых названий групп
        private string GetDateGroupName(DateTime dt)
        {
            if (dt.Date == DateTime.Today) return "сегодня";
            if (dt.Date == DateTime.Today.AddDays(-1)) return "вчера";
            return dt.ToString("dd.MM.yyyy");
        }

        private void RecordingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgressBar.Value = 0;
            TimeLabel.Text = "00:00 / 00:00";
            _progressTimer.Stop();
            Player.Stop();
            _isPlaying = false;

            UpdateAppBarButtonsState();
        }

        private void RecordingsSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, что выбрана именно запись, а не пустой заголовок
            if (RecordingsSelector.SelectedItem is RecordingInfo)
            {
                _selectedRecording = (RecordingInfo)RecordingsSelector.SelectedItem;

                // Если выбрали новый файл, а старый еще играет — сбрасываем плеер
                if (_currentPlayingFile != _selectedRecording.FileName)
                {
                    Player.Stop();
                    Player.Source = null;
                    _currentPlayingFile = null;
                    ProgressBar.Value = 0;
                    TimeLabel.Text = "00:00 / 00:00";
                    _isPlaying = false;
                }

                UpdateAppBarButtonsState();
            }
        }


        private void UpdateAppBarButtonsState()
        {
            bool hasSelection = _selectedRecording != null;

            var rewindButton = ApplicationBar.Buttons[0] as ApplicationBarIconButton;
            var playButton = ApplicationBar.Buttons[1] as ApplicationBarIconButton;
            var pauseButton = ApplicationBar.Buttons[2] as ApplicationBarIconButton;
            var saveButton = ApplicationBar.Buttons[3] as ApplicationBarIconButton;

            var renameMenuItem = ApplicationBar.MenuItems[0] as ApplicationBarMenuItem;
            var deleteMenuItem = ApplicationBar.MenuItems[1] as ApplicationBarMenuItem;

            if (rewindButton != null)
                rewindButton.IsEnabled = hasSelection; // Кнопка "в начало" активна всегда, когда выбрана запись

            if (playButton != null)
                playButton.IsEnabled = hasSelection && !_isPlaying; // доступна только когда есть выбор и НЕ играет

            if (pauseButton != null)
                pauseButton.IsEnabled = hasSelection && _isPlaying; // доступна только когда играет

            if (saveButton != null)
                saveButton.IsEnabled = hasSelection;                // всегда при наличии выбора

            if (renameMenuItem != null)
                renameMenuItem.IsEnabled = hasSelection;
            if (deleteMenuItem != null)
                deleteMenuItem.IsEnabled = hasSelection;
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null)
            {
                MessageBox.Show("Выбери запись");
                return;
            }

            if (Player.Source != null && _currentPlayingFile == _selectedRecording.FileName)
            {
                Player.Play();
                _isPlaying = true;
                _progressTimer.Start();
                UpdateAppBarButtonsState();
                return;
            }

            try
            {
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!iso.FileExists(_selectedRecording.FileName))
                    {
                        MessageBox.Show("Файл не найден");
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
                MessageBox.Show("Ошибка воспроизведения: " + ex.Message, "Ой-ёй..", MessageBoxButton.OK);
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            Player.Play();
            _isPlaying = true;
            _progressTimer.Start();
            UpdateAppBarButtonsState();
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (Player.CurrentState == MediaElementState.Playing)
            {
                Player.Pause();
                _progressTimer.Stop();
                _isPlaying = false;
            }
            else if (Player.CurrentState == MediaElementState.Paused)
            {
                Player.Play();
                _progressTimer.Start();
                _isPlaying = true;
            }

            UpdateAppBarButtonsState();
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null) return;

            if (MessageBox.Show("Удалить запись?", "Ты уверен?",
                MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;

            try
            {
                _progressTimer.Stop();
                Player.Stop();
                Player.Source = null;
                _isPlaying = false;

                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (iso.FileExists(_selectedRecording.FileName))
                    {
                        iso.DeleteFile(_selectedRecording.FileName);
                    }
                }

                LoadRecordings();
                UpdateAppBarButtonsState();

                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ой!", MessageBoxButton.OK);
            }
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null)
            {
                MessageBox.Show("Выбери запись");
                return;
            }

            PromptForRenameAsync(_selectedRecording.DisplayName);
        }

        private void PromptForRenameAsync(string currentName)
        {
            var textBox = new TextBox
            {
                Text = currentName,
                AcceptsReturn = false,
                TextWrapping = TextWrapping.Wrap
            };

            textBox.GotFocus += (s, e) =>
            {
                textBox.SelectAll();
            };

            var stackPanel = new StackPanel();
            var label = new TextBlock
            {
                Text = "НЕ оставляйте цифры в названии!",
                Margin = new Thickness(10, 10, 10, 8)
            };
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);

            var dialog = new CustomMessageBox
            {
                Title = "Переименовать запись",
                Content = stackPanel,
                LeftButtonContent = "ОК",
                RightButtonContent = "Отмена",
                IsFullScreen = false
            };

            dialog.Dismissed += (s, e) =>
            {
                if (e.Result == CustomMessageBoxResult.LeftButton)
                {
                    string newName = textBox.Text;

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Имя не может быть пустым");
                        return;
                    }

                    var invalidChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
                    if (newName.Any(c => invalidChars.Contains(c)))
                    {
                        MessageBox.Show("Имя содержит недопустимые символы");
                        return;
                    }

                    PerformRename(newName);
                }
            };

            dialog.Show();
        }

        private void PerformRename(string newName)
        {
            try
            {
                _progressTimer.Stop();
                Player.Stop();
                Player.Source = null;
                _isPlaying = false;

                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string oldFileName = _selectedRecording.FileName;
                    // recording_20260109_1234.wav → recording_20260109_1234_newName.wav
                    string newFileName = oldFileName.Substring(0, oldFileName.Length - 4) + "_" + newName + ".wav";

                    if (iso.FileExists(oldFileName))
                    {
                        // Если файл с новым именем уже существует, удаляем его
                        if (iso.FileExists(newFileName))
                        {
                            try
                            {
                                iso.DeleteFile(newFileName);
                            }
                            catch { }
                        }

                        // Копируем файл
                        using (var sourceStream = iso.OpenFile(oldFileName, FileMode.Open, FileAccess.Read))
                        using (var targetStream = iso.CreateFile(newFileName))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                targetStream.Write(buffer, 0, bytesRead);
                            }
                            targetStream.Flush();
                        }

                        // Удаляем старый файл
                        iso.DeleteFile(oldFileName);

                        LoadRecordings();
                        _selectedRecording = null;
                        _selectedTextBlock = null;
                        UpdateAppBarButtonsState();
                        MessageBox.Show("Запись переименована", "Успех!", MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButton.OK);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при переименовании: " + ex.Message, "Ой!", MessageBoxButton.OK);
            }
        }

        private string PromptForRename(string currentName)
        {
            var textBox = new TextBox
            {
                Text = currentName,
                AcceptsReturn = false,
                TextWrapping = TextWrapping.Wrap
            };

            var stackPanel = new StackPanel();
            var label = new TextBlock
            {
                Text = "Новое имя записи:",
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);

            var dialog = new CustomMessageBox
            {
                Title = "Переименовать запись",
                Content = stackPanel,
                LeftButtonContent = "ОК",
                RightButtonContent = "Отмена",
                IsFullScreen = false
            };

            string result = null;
            dialog.Dismissed += (s, e) =>
            {
                if (e.Result == CustomMessageBoxResult.LeftButton)
                {
                    result = textBox.Text;
                }
            };

            dialog.Show();
            return result;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                var total = Player.NaturalDuration.TimeSpan;
                var current = Player.Position;

                ProgressBar.Maximum = 100;
                ProgressBar.Value = (current.TotalSeconds / total.TotalSeconds) * 100;

                TimeLabel.Text = string.Format("{0:mm\\:ss} / {1:mm\\:ss}",
                    current, total);
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _progressTimer.Stop();
            ProgressBar.Value = 0;
            TimeLabel.Text = "00:00 / 00:00";
            _isPlaying = false;
            UpdateAppBarButtonsState();
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Ошибка при воспроизведении: " + (e.ErrorException != null ? e.ErrorException.Message : "неизвестная ошибка"),
                "Досадно..", MessageBoxButton.OK);
            _progressTimer.Stop();
            _isPlaying = false;
            UpdateAppBarButtonsState();
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (_selectedRecording == null) return;

            try
            {
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!iso.FileExists(_selectedRecording.FileName))
                    {
                        MessageBox.Show("Файл не найден", "А где?", MessageBoxButton.OK);
                        return;
                    }

                    byte[] buffer;
                    using (var fileStream = iso.OpenFile(_selectedRecording.FileName, FileMode.Open, FileAccess.Read))
                    {
                        buffer = new byte[fileStream.Length];
                        fileStream.Read(buffer, 0, buffer.Length);
                    }

                    var musicFolder = KnownFolders.MusicLibrary;
                    var appFolder = await musicFolder.CreateFolderAsync("SimpleDictaphone",
                        CreationCollisionOption.OpenIfExists);

                    var file = await appFolder.CreateFileAsync(_selectedRecording.FileName,
                        CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteBytesAsync(file, buffer);

                    MessageBox.Show("Запись сохранена в Music/SimpleDictaphone",
                        "Успех!", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Как так то?", MessageBoxButton.OK);
            }
        }

        private async void SaveRecordingToMusicLibrary(string fileName, byte[] buffer)
        {
            try
            {
                var musicFolder = Windows.Storage.KnownFolders.MusicLibrary;

                var appFolder = await musicFolder.CreateFolderAsync("SimpleDictaphone",
                    Windows.Storage.CreationCollisionOption.OpenIfExists);

                var file = await appFolder.CreateFileAsync(fileName,
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    stream.Write(buffer, 0, buffer.Length);
                }

                MessageBox.Show("Запись сохранена в папку Music/SimpleDictaphone", "Успех!", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении в музыку: " + ex.Message, "Как так то?", MessageBoxButton.OK);
            }
        }

        private void RewindButton_Click(object sender, EventArgs e)
        {
            if (Player != null)
            {
                Player.Position = TimeSpan.Zero; // Перематываем в 00:00

                // Обновляем UI сразу, не дожидаясь тика таймера
                ProgressBar.Value = 0;
                TimeLabel.Text = string.Format("00:00 / {0:mm\\:ss}", Player.NaturalDuration.TimeSpan);
            }
        }
    }
}
