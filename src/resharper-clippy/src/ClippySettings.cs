using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Utils;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SettingsKey(typeof (UserInterfaceSettings), "Clippy settings")]
    public class ClippySettings
    {
        [SettingsEntry(true, "Enable sounds effects")]
        public bool SoundEffects { get; set; }
    }

    [ShellComponent]
    public class ClippySettingsStore(ISettingsStore settingsStore, DataContexts dataContexts)
    {
        public ClippySettings GetSettings()
        {
            var boundSettings = BindSettingsStore();
            return boundSettings.GetKey<ClippySettings>(SettingsOptimization.OptimizeDefault);
        }

        private IContextBoundSettingsStore BindSettingsStore()
        {
            var store = settingsStore.BindToContextTransient(ContextRange.Smart((l, _) => dataContexts.CreateOnSelection(l)));
            return store;
        }

        // Set-tastic
        public void SetSettings(ClippySettings settings)
        {
            var boundSettings = BindSettingsStore();
            boundSettings.SetKey(settings, SettingsOptimization.OptimizeDefault);
        }
    }
}