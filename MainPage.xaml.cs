using Windows.Media.Control;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AudioCopilot
{
    public partial class MainPage : ContentPage
    {
        private GlobalSystemMediaTransportControlsSessionManager _sessionManager;
        private GlobalSystemMediaTransportControlsSession _currentSession;
        private System.Timers.Timer _updateTimer;
        private bool _isUpdatingProgress = false;
        private DateTime _lastUpdateTimestamp = DateTime.MinValue;

        public string SongTitle { get; set; }
        public string Artist { get; set; }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            InitializeMediaSession();
            InitializeUpdateTimer();
        }

        private void InitializeUpdateTimer()
        {
            _updateTimer = new System.Timers.Timer(100);
            _updateTimer.Elapsed += async (s, e) => await UpdateMediaProgress();
            _updateTimer.Start();
        }

        private async void InitializeMediaSession()
        {
            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

                if (_sessionManager != null)
                {
                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                    UpdateCurrentSession();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al inicializar la sesión de medios: {ex.Message}");
            }
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentSession();
            _lastUpdateTimestamp = DateTime.MinValue;
        }

        private void UpdateCurrentSession()
        {
            _currentSession = _sessionManager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
                _lastUpdateTimestamp = DateTime.MinValue;

                // Actualizar UI inmediatamente
                _ = GetMediaProperties();
                _ = UpdateMediaProgress();
            }
            else
            {
                // Si no hay sesión, limpia la UI
                SongTitle = "No se está reproduciendo música";
                Artist = string.Empty;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    currentTimeLabel.Text = "0:00";
                    totalTimeLabel.Text = "0:00";
                    progressBar.Progress = 0;
                    albumImage.Source = "default_image.png";
                });
                OnPropertyChanged(nameof(SongTitle));
                OnPropertyChanged(nameof(Artist));
            }
        }

        private async void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            _lastUpdateTimestamp = DateTime.MinValue;
            await UpdateMediaProgress();
        }

        private async Task UpdateMediaProgress()
        {
            if (_isUpdatingProgress || _currentSession == null)
                return;

            _isUpdatingProgress = true;

            try
            {
                var timelineProperties = _currentSession.GetTimelineProperties();
                var playbackInfo = _currentSession.GetPlaybackInfo();

                if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    var currentPosition = timelineProperties.Position;
                    var totalDuration = timelineProperties.EndTime - timelineProperties.StartTime;

                    // Calcula el tiempo transcurrido desde la última actualización
                    if (_lastUpdateTimestamp != DateTime.MinValue)
                    {
                        var elapsed = DateTime.Now - _lastUpdateTimestamp;
                        currentPosition = currentPosition.Add(elapsed);
                    }

                    if (totalDuration.TotalSeconds > 0)
                    {
                        double progress = Math.Min(currentPosition.TotalSeconds / totalDuration.TotalSeconds, 1.0);

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            progressBar.Progress = progress;
                            currentTimeLabel.Text = $"{(int)currentPosition.TotalMinutes}:{currentPosition.Seconds:D2}";
                            totalTimeLabel.Text = $"{(int)totalDuration.TotalMinutes}:{totalDuration.Seconds:D2}";
                        });
                    }
                }

                _lastUpdateTimestamp = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en UpdateMediaProgress: {ex.Message}");
            }
            finally
            {
                _isUpdatingProgress = false;
            }
        }

        private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await GetMediaProperties();
        }

        private async Task GetMediaProperties()
        {
            if (_currentSession == null)
                return;

            try
            {
                var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();

                if (mediaProperties != null)
                {
                    SongTitle = mediaProperties.Title ?? "Desconocido";
                    Artist = mediaProperties.Artist ?? "Artista desconocido";

                    if (mediaProperties.Thumbnail != null)
                    {
                        try
                        {
                            var thumbnailFile = mediaProperties.Thumbnail;
                            var stream = await thumbnailFile.OpenReadAsync();
                            var imageSource = ImageSource.FromStream(() => stream.AsStream());

                            await MainThread.InvokeOnMainThreadAsync(() => albumImage.Source = imageSource);
                        }
                        catch
                        {
                            await MainThread.InvokeOnMainThreadAsync(() => albumImage.Source = "default_image.png");
                        }
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => albumImage.Source = "default_image.png");
                    }

                    OnPropertyChanged(nameof(SongTitle));
                    OnPropertyChanged(nameof(Artist));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener propiedades del medio: {ex.Message}");
            }
        }

        private async void OnPlayPauseClicked(object sender, EventArgs e)
        {
            if (_currentSession != null)
            {
                try
                {
                    var playbackInfo = _currentSession.GetPlaybackInfo();

                    if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        await _currentSession.TryPauseAsync();
                    }
                    else
                    {
                        await _currentSession.TryPlayAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al pausar o reproducir: {ex.Message}");
                }
            }
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            if (_currentSession != null)
            {
                try
                {
                    await _currentSession.TrySkipNextAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al saltar a la siguiente canción: {ex.Message}");
                }
            }
        }

        private async void OnPreviousClicked(object sender, EventArgs e)
        {
            if (_currentSession != null)
            {
                try
                {
                    await _currentSession.TrySkipPreviousAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al retroceder a la canción anterior: {ex.Message}");
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _updateTimer?.Stop();
            _updateTimer?.Dispose();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }
        }
    }
}