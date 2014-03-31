using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurences.Presentation;
using JetBrains.ReSharper.Features.Environment.RecentFiles;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.UI.PopupWindowManager;
using GotoRecentEditsAction = CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions.GotoRecentEditsAction;
using GotoRecentFilesAction = CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions.GotoRecentFilesAction;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class OverridingActionRegistrar
    {
        public OverridingActionRegistrar(Lifetime lifetime, Agent agent, ISolution solution,
            IShellLocks shellLocks, IActionManager actionManager, IShortcutManager shortcutManager,
            IPsiFiles psiFiles, RecentFilesTracker tracker, OccurencePresentationManager presentationManager,
            MainWindowPopupWindowContext mainWindowPopupWindowContext)
        {
            RegisterHandler(actionManager, "RefactorThis", lifetime,
                new RefactorThisAction(lifetime, agent, actionManager, shortcutManager));
            RegisterHandler(actionManager, "NavigateTo", lifetime,
                new NavigateFromHereAction(lifetime, agent, actionManager, shortcutManager));
            RegisterHandler(actionManager, "Generate", lifetime,
                new GenerateAction(lifetime, agent, actionManager, shortcutManager));
            RegisterHandler(actionManager, "GenerateFileBesides", lifetime, 
                new FileTemplatesGenerateAction(lifetime, agent, actionManager, shortcutManager));

            RegisterHandler(actionManager, "GotoRecentFiles", lifetime,
                new GotoRecentFilesAction(lifetime, agent, solution, shellLocks, psiFiles, tracker, presentationManager, mainWindowPopupWindowContext));
            RegisterHandler(actionManager, "GotoRecentEdits", lifetime,
                new GotoRecentEditsAction(lifetime, agent, solution, shellLocks, psiFiles, tracker, presentationManager, mainWindowPopupWindowContext));
        }

        private static void RegisterHandler(IActionManager actionManager, string actionId, Lifetime lifetime, IActionHandler handler)
        {
            var action = actionManager.GetExecutableAction(actionId);
            if (action != null)
                action.AddHandler(lifetime, handler);
        }
    }
}