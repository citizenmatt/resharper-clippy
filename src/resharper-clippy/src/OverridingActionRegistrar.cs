using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Features.Navigation.Core.RecentFiles;
using JetBrains.ReSharper.Psi.Files;
using GotoRecentEditsAction = CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions.GotoRecentEditsAction;
using GotoRecentFilesAction = CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions.GotoRecentFilesAction;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class OverridingActionRegistrar
    {
        public OverridingActionRegistrar(Lifetime lifetime, Agent agent, ISolution solution,
            IShellLocks shellLocks, IActionManager actionManager, IThreading threading,
            IPsiFiles psiFiles, RecentFilesTracker tracker, OccurrencePresentationManager presentationManager,
            ProjectModelElementPointerManager projectModelElementPointerManager,
            IMainWindowPopupWindowContext mainWindowPopupWindowContext)
        {
            RegisterHandler(actionManager, "RefactorThis", lifetime,
                new RefactorThisAction(lifetime, agent, actionManager));
            RegisterHandler(actionManager, "NavigateTo", lifetime,
                new NavigateFromHereAction(lifetime, agent, actionManager));
            RegisterHandler(actionManager, "Generate", lifetime,
                new GenerateAction(lifetime, agent, actionManager));
            RegisterHandler(actionManager, "GenerateFileBesides", lifetime,
                new FileTemplatesGenerateAction(lifetime, agent, actionManager));
            RegisterHandler(actionManager, "InspectThis", lifetime,
                new InspectThisAction(lifetime, agent, actionManager, threading));

            RegisterHandler(actionManager, "GotoRecentFiles", lifetime,
                new GotoRecentFilesAction(lifetime, agent, solution, shellLocks, psiFiles, tracker, presentationManager, projectModelElementPointerManager, mainWindowPopupWindowContext));
            RegisterHandler(actionManager, "GotoRecentEdits", lifetime,
                new GotoRecentEditsAction(lifetime, agent, solution, shellLocks, psiFiles, tracker, presentationManager, projectModelElementPointerManager, mainWindowPopupWindowContext));
        }

        private static void RegisterHandler(IActionManager actionManager, string actionId, Lifetime lifetime, IAction handler)
        {
            var def = actionManager.Defs.TryGetActionDefById(actionId);
            if (def != null)
                actionManager.Handlers.AddHandler(lifetime, def, handler);
        }
    }
}