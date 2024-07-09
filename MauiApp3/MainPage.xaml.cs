using CommunityToolkit.Maui.Views;

namespace MauiApp3
{
    public partial class MainPage : ContentPage
    {
       

        public MainPage()
        {
            InitializeComponent();
        }

        void OnZoomInClicked(object? sender, EventArgs e)
        {
            imageEl.Scale += 0.1;
        }

        void OnZoomOutClicked(object? sender, EventArgs e)
        {

            imageEl.Scale -= 0.1;
        }

        void OnPlayClicked(object? sender, EventArgs e)
        {
            mediaElement.Play();
        }

        void OnPauseClicked(object? sender, EventArgs e)
        {
            mediaElement.Pause();
        }

        void OnStopClicked(object? sender, EventArgs e)
        {
            mediaElement.Stop();
        }

        void OnMuteClicked(object? sender, EventArgs e)
        {
            mediaElement.ShouldMute = !mediaElement.ShouldMute;
        }

        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            mediaElement.Stop();
            mediaElement.Handler?.DisconnectHandler();
        }

    }

}
