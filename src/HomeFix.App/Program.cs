using System.Globalization;
using HomeFix.App.Demo;
using HomeFix.App.Input;
using HomeFix.App.Output;
using HomeFix.App.Screens;
using HomeFix.Core.Workshop;

namespace HomeFix.App
{
    /// <summary>
    /// Starts the program. With <c>--tour</c> it tells the guided tour and stops; otherwise it plays
    /// the tour silently, so the workshop already holds jobs at several stages, and opens the menu.
    /// </summary>
    internal static class Program
    {
        private const string TourSwitch = "--tour";

        private static int Main(string[] args)
        {
            // A fixed culture: hours, amounts and dates print the same on every PC (1.5 h,
            // 409,375 SYP, 05/11/2026, Gregorian) instead of following the regional settings.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            DateTime today = DateTime.Today;

            if (args.Contains(TourSwitch, StringComparer.OrdinalIgnoreCase))
            {
                GuidedTour.Play(Console.Out, today, workshop => ConnectAlerts(workshop, Console.Out));
                return 0;
            }

            ServiceWorkshop sample = GuidedTour.Play(TextWriter.Null, today);
            ConnectAlerts(sample, Console.Out);

            try
            {
                new MainMenu(sample, new ConsolePrompt(Console.In, Console.Out), Console.Out, () => DateTime.Today).Run();
            }
            catch (EndOfStreamException)
            {
                // Standard input ran out (a redirected file, or Ctrl+Z): end without an error.
            }

            return 0;
        }

        // The two methods of the printer match the two delegates, so they subscribe to the events.
        private static void ConnectAlerts(ServiceWorkshop workshop, TextWriter output)
        {
            var alerts = new AlertPrinter(output);
            workshop.RepairFinished += alerts.OnRepairFinished;
            workshop.DeadlineMissed += alerts.OnDeadlineMissed;
        }
    }
}
