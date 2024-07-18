using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.ActionsRevised.Loader;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.DataContext;
using JetBrains.Lifetimes;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public class InspectThisAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IThreading threading)
        : IExecutableAction
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            if (context.GetData(UIDataConstants.PopupWindowContextSource) == null)
                return false;

            var menuActionGroup = GetMenuActionGroup();
            return menuActionGroup != null
                   && menuActionGroup.MenuChildren.OfType<IActionDefWithId>().Any(executableAction => actionManager.Handlers.Evaluate(executableAction, context).IsAvailable);
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            var actionGroup = GetMenuActionGroup();
            if (actionGroup == null)
                return;

            var lifetimeDefinition = lifetime.CreateNested();

            var options = new List<BalloonOption>();
            foreach (var action in GetAvailableActions(actionGroup, context, actionManager))
            {
                var text = GetCaption(action, context, actionManager);
                options.Add(new BalloonOption(text, action));
            }

            agent.ShowBalloon(lifetimeDefinition.Lifetime, "Inspect This", string.Empty,
                options, ["Done"], true,
                balloonLifetime =>
                {
                    agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                    {
                        lifetimeDefinition.Terminate();

                        if (o is IActionDefWithId action)
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