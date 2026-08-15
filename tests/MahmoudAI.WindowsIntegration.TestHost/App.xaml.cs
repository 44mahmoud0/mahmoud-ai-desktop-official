using Microsoft.UI.Xaml;

namespace MahmoudAI.WindowsIntegration.TestHost;

public class App : Application
{
    private Window? _window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    public static void Main(string[] args)
    {
        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
