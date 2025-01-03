using Windows.Media.Control;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;


namespace AudioCopilot
{
    public partial class MainPage : ContentPage
    {
        private GlobalSystemMediaTransportControlsSessionManager _sessionManager;
        private GlobalSystemMediaTransportControlsSession _currentSession;
        private Timer _playbackTimer;

        // Propiedades para el título y el artista de la canción
        public string SongTitle { get; set; }
        public string Artist { get; set; }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this; // Establece el BindingContext de la página
            InitializeMediaSession();
        }

        private async void InitializeMediaSession()
        {
            try
            {
                // Obtén el administrador de sesiones de medios globales
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

                if (_sessionManager != null)
                {
                    // Suscríbete a los cambios en la sesión actual
                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

                    // Configura la sesión inicial
                    UpdateCurrentSession();
                    StartPlaybackTimer();

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
        }

        private void UpdateCurrentSession()
        {
            _currentSession = _sessionManager?.GetCurrentSession();

            if (_currentSession != null)
            {
                // Suscríbete a los cambios en las propiedades del medio
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;

                // Actualiza las propiedades iniciales
                GetMediaProperties();
            }
            else
            {
                // Limpia los datos si no hay sesión actual
                SongTitle = "No se está reproduciendo música";
                Artist = string.Empty;
                OnPropertyChanged(nameof(SongTitle));
                OnPropertyChanged(nameof(Artist));
            }
        }

        private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await GetMediaProperties();
        }

        private void StartPlaybackTimer()
        {
            _playbackTimer = new Timer(200); // Intervalo de 200 ms para mayor precisión
            _playbackTimer.Elapsed += async (sender, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LogPlaybackInfo();
                });
            };
            _playbackTimer.Start();
        }



        private DateTime _lastUpdateTimestamp;


        private async Task LogPlaybackInfo()
        {
            if (_currentSession != null)
            {
                try
                {
                    var playbackInfo = _currentSession.GetPlaybackInfo();
                    var timelineProperties = _currentSession.GetTimelineProperties();

                    var currentPosition = timelineProperties.Position +
                                          (DateTime.Now - _lastUpdateTimestamp); // Ajusta el tiempo transcurrido
                    var totalDuration = timelineProperties.EndTime - timelineProperties.StartTime;

                    if (totalDuration.TotalSeconds > 0)
                    {
                        // Calcula el porcentaje de progreso
                        double progress = (currentPosition.TotalSeconds / totalDuration.TotalSeconds) * 100;

                        // Actualiza la UI
                        progressBar.Value = progress;
                        currentTimeLabel.Text = $"{currentPosition.Minutes}:{currentPosition.Seconds:D2}";
                        totalTimeLabel.Text = $"{totalDuration.Minutes}:{totalDuration.Seconds:D2}";
                    }

                    _lastUpdateTimestamp = DateTime.Now; // Actualiza el último timestamp
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al obtener información de reproducción: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("No hay sesión activa.");
            }
        }






        private async Task GetMediaProperties()
        {
            if (_currentSession != null)
            {

                try
                {
                    var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();

                    if (mediaProperties != null)
                    {
                        // Actualiza las propiedades (Título, ArtistAa)
                        SongTitle = mediaProperties.Title ?? "Desconocido";
                        Artist = mediaProperties.Artist ?? "Artista desconocido";

                        // Actualiza la miniatura si está disponible
                        if (mediaProperties.Thumbnail != null)
                        {
                            try
                            {
                                var thumbnailFile = mediaProperties.Thumbnail;
                                var stream = await thumbnailFile.OpenReadAsync();
                                var imageSource = ImageSource.FromStream(() => stream.AsStream());

                                // Actualiza la imagen en el hilo principal
                                Dispatcher.Dispatch(() =>
                                {
                                    albumImage.Source = imageSource;
                                });
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error al cargar la miniatura: {ex.Message}");

                                // Actualiza la imagen predeterminada en el hilo principal
                                Dispatcher.Dispatch(() =>
                                {
                                    albumImage.Source = "default_image.png";
                                });
                            }
                        }
                        else
                        {
                            // Actualiza la imagen predeterminada en el hilo principal
                            Dispatcher.Dispatch(() =>
                            {
                                albumImage.Source = "default_image.png";
                            });
                        }


                        // Notifica cambios a la UI
                        OnPropertyChanged(nameof(SongTitle));
                        OnPropertyChanged(nameof(Artist));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al obtener propiedades del medio: {ex.Message}");
                }
            }
            else
            {
                // Limpia la información si no hay sesión
                SongTitle = "No se está reproduciendo música";
                Artist = string.Empty;
                albumImage.Source = null;

                OnPropertyChanged(nameof(SongTitle));
                OnPropertyChanged(nameof(Artist));
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
                        Debug.WriteLine("Audio pausado.");
                    }
                    else
                    {
                        await _currentSession.TryPlayAsync();
                        Debug.WriteLine("Reproduciendo audio.");
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
                    Debug.WriteLine("Avanzando a la siguiente canción.");
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
                    Debug.WriteLine("Volviendo a la canción anterior.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al retroceder a la canción anterior: {ex.Message}");
                }
            }
        }
    }
}
