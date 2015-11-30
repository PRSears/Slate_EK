using Extender.Debugging;
using Extender.IO;
using Slate_EK.Models.IO;
using Slate_EK.Views;
using System;
using System.Linq;
using System.Windows;

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
            // Caps framerate at 6fps.
            System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(System.Windows.Media.Animation.Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 6 });


            //
            // Setup console output
            if (RedirectConsole && !string.IsNullOrWhiteSpace(DebugLogPath))
            {
                var logStream = new System.IO.StreamWriter(DebugLogPath, true)           {AutoFlush = true};
                var standard  = new System.IO.StreamWriter(Console.OpenStandardOutput()) {AutoFlush = true};

                Console.SetOut(new ActionTextWriter
                (
                    (text) =>
                    {
                        standard.Write(text);
                        logStream.Write(text);
                    }
                ));
            }

            Debug.WriteMessage($"Startup: Args({e.Args.Length}) = {string.Join(", ", e.Args)}");

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

            BuildCaches();
        }

        private void BuildCaches()
        {
            Debug.WriteMessage(SizesCache.IsBuilt()         ? "Sizes cache built."          : "Building Sizes cache...", "info");
            Debug.WriteMessage(PitchesCache.IsBuilt()       ? "Pitches cache built."        : "Building Pitches cache...", "info");
            Debug.WriteMessage(ImperialSizesCache.IsBuilt() ? "ImperialSizes cache built."  : "Building ImperialSizes cache...", "info");
        }

        private static bool   RedirectConsole => Slate_EK.Properties.Settings.Default.DebugRedirectConsoleOut;
        private static string DebugLogPath    => Slate_EK.Properties.Settings.Default.DebugFilename;
    }
}
