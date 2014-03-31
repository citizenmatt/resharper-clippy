using System.Windows.Input;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public static class Commands
    {
        public readonly static RoutedCommand SeeNextCommand = new RoutedCommand();
        public readonly static RoutedCommand SeePreviousCommand = new RoutedCommand();
        public readonly static RoutedCommand OptionCommand = new RoutedCommand();
        public readonly static RoutedCommand ButtonCommand = new RoutedCommand();
    }
}