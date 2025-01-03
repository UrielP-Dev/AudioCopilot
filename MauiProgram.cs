using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
namespace AudioCopilot
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows =>
                    windows.OnWindowCreated((window) =>
                    {
                        // Obtener el identificador de la ventana
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var windowId = Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = AppWindow.GetFromWindowId(windowId);

                        // Establecer tamaño fijo
                        var size = new SizeInt32
                        {
                            Width = 800,  // Ancho en píxeles
                            Height = 300  // Alto en píxeles
                        };
                        appWindow.Resize(size);

                        // Configurar ventana sin bordes (eliminar barra superior)
                        appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay); // Sin bordes ni barra

                        // Configurar opciones adicionales del presentador
                        if (appWindow.Presenter is OverlappedPresenter presenter)
                        {
                            //presenter.IsResizable = false;     // Deshabilitar cambiar tamaño
                            //presenter.IsMaximizable = false;  // Deshabilitar maximizar
                        }
                    }));
#endif
            });

            return builder.Build();
        }
    }
}
