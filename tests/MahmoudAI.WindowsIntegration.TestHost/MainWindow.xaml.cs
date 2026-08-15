using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MahmoudAI.WindowsIntegration.TestHost;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        var grid = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var text = new TextBlock
        {
            Text = "MahmoudAI Test Host",
            FontSize = 24,
            Margin = new Thickness(20)
        };

        // Add some colorful elements for pixel diversity tests
        var canvas = new Canvas { Width = 200, Height = 200 };
        canvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle { Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)), Width = 50, Height = 50 });
        canvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle { Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)), Width = 50, Height = 50, Margin = new Thickness(50, 0, 0, 0) });
        canvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle { Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)), Width = 50, Height = 50, Margin = new Thickness(0, 50, 0, 0) });
        canvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle { Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)), Width = 50, Height = 50, Margin = new Thickness(50, 50, 0, 0) });

        stack.Children.Add(text);
        stack.Children.Add(canvas);
        grid.Children.Add(stack);

        this.Content = grid;
        this.Title = "MahmoudAI.WindowsIntegration.TestHost";
    }
}
