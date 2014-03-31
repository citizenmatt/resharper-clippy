using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions;
using JetBrains.ActionManagement;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class OverridingActionRegistrar
    {
        public OverridingActionRegistrar(Lifetime lifetime,
            Agent agent, IActionManager actionManager, IShortcutManager shortcutManager)
        {
            RegisterHandler(actionManager, "NavigateTo", lifetime,
                new NavigateFromHereAction(lifetime, agent, actionManager, shortcutManager));
            RegisterHandler(actionManager, "Generate", lifetime,
                new GenerateAction(lifetime, agent, actionManager, shortcutManager));
            RegisterHandler(actionManager, "GenerateFileBesides", lifetime, 
                new FileTemplatesGenerateAction(lifetime, agent, actionManager, shortcutManager));
        }

        private static void RegisterHandler(IActionManager actionManager, string actionId, Lifetime lifetime, IActionHandler handler)
        {
            var action = actionManager.GetExecutableAction(actionId);
            if (action != null)
                action.AddHandler(lifetime, handler);
        }
    }
}