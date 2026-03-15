using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace GymVisual;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Database.EnsureSchema();
        }
        catch (Exception ex)
        {
            // Mostrar un mensaje de error en caso de fallo de conexión o esquema faltante.
            var dlg = new Window
            {
                Title = "Error de base de datos",
                Width = 480,
                Height = 220,
                Content = new TextBlock
                {
                    Text = "No se pudo inicializar la base de datos:\n" + ex.Message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(16)
                },
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // Mostramos el diálogo y dejamos que el usuario siga (la app seguirá abierta).
            dlg.Show();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
//git add .
//git commit -m "Descripción de lo que cambiaste (ej: Diseño del menú principal)"
//git push origin main