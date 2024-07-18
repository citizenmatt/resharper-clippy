using System.Windows.Input;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public static class Commands
    {
        public static readonly RoutedCommand SeeNextCommand = new RoutedCommand();
        public static readonly RoutedCommand SeePreviousCommand = new RoutedCommand();
        public static readonly RoutedCommand OptionCommand = new RoutedCommand();
        public static readonly RoutedCommand ButtonCommand = new RoutedCommand();
    }
}