using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.Threading;
using JetBrains.UI.ActionsRevised;
using JetBrains.UI.ActionsRevised.Handlers;
using JetBrains.UI.ActionsRevised.Loader;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public class InspectThisAction : IExecutableAction
    {
        private readonly Lifetime lifetime;
        private readonly Agent agent;
        private readonly IActionManager actionManager;
        private readonly IThreading threading;

        public InspectThisAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IThreading threading)
        {
            this.lifetime = lifetime;
            this.agent = agent;
            this.actionManager = actionManager;
            this.threading = threading;
        }

        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            if (context.GetData(JetBrains.UI.DataConstants.PopupWindowContextSource) == null)
                return false;

            var menuActionGroup = GetMenuActionGroup();
            if (menuActionGroup == null)
                return false;

            return menuActionGroup.MenuChildren.OfType<IActionDefWithId>().Any(executableAction => actionManager.Handlers.Evaluate(executableAction, context).IsAvailable);
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            var actionGroup = GetMenuActionGroup();
            if (actionGroup == null)
                return;

            var lifetimeDefinition = Lifetimes.Define(lifetime);

            var options = new List<BalloonOption>();
            foreach (var action in GetAvailableActions(actionGroup, context, actionManager))
            {
                var text = GetCaption(action, context, actionManager);
                options.Add(new BalloonOption(text, action));
            }

            agent.ShowBalloon(lifetimeDefinition.Lifetime, "Inspect This", string.Empty,
                options, new [] { "Done" }, true,
                balloonLifetime =>
                {
                    agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                    {
                        lifetimeDefinition.Terminate();

                        var action = o as IActionDefWithId;
                        threading.ExecuteOrQueue("InspectThisItem", () => action.EvaluateAndExecute(actionManager));
                    });

                    agent.ButtonClicked.Advise(balloonLifetime, _ => lifetimeDefinition.Terminate());
                });
        }

        private IActionGroupDef GetMenuActionGroup()
        {
            return actionManager.Defs.TryGetActionDefById("InspectMenu") as IActionGroupDef;
        }

        private static IEnumerable<IActionDefWithId> GetAvailableActions(IActionGroupDef actionGroup, IDataContext dataContext, IActionManager actionManager)
        {
            return
                actionGroup.MenuChildren.OfType<IActionDefWithId>()
                    .Where(
                        executableAction => actionManager.Handlers.Evaluate(executableAction, dataContext).IsAvailable)
                    .ToList();
        }

        private static string GetCaption(IActionDefWithId action, IDataContext dataContext, IActionManager actionManager)
        {
            return actionManager.Handlers.Evaluate(action, dataContext).Text;
        }
    }
}