using System.Linq;
using System;
using Extender;
using Extender.Debugging;
using System.Windows;
using Slate_EK.Views;

namespace Slate_EK
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // "Workaround" to reduce lag while typing
            // Caps framerate at 5fps.
            System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(System.Windows.Media.Animation.Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 5 });

            Window main;

            if (e.Args.Any(a => a.ToLower().Contains("inv")))
            {
                if (e.Args.Any(a => a.ToLower().Contains("only")))
                    main = new InventoryView();
                else
                    main = new MainView(new InventoryView());
            }
            else
                main = new MainView();

            main.Show();
        }
    }
}
