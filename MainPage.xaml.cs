using System.Diagnostics;

namespace AudioCopilot
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            Debug.WriteLine("Este es un mensaje de depuración.");
        }

        // Event handler for "Previous" button
        private void OnPreviousClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Este es un mensaje de depuración.");
        }

        // Event handler for "Play/Pause" button
        private void OnPlayPauseClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Este es un mensaje de depuración.");
        }

        // Event handler for "Next" button
        private void OnNextClicked(object sender, EventArgs e)
        {
            Console.WriteLine("Next track clicked");
        }

    }
}
