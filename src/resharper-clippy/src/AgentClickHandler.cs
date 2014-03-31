using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Threading;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [ShellComponent]
    public class AgentClickHandler
    {
        private readonly IActionManager actionManager;
        private readonly IShortcutManager shortcutManager;
        private readonly IThreading threading;

        public AgentClickHandler(Lifetime lifetime, Agent agent,
            IActionManager actionManager, IShortcutManager shortcutManager, IThreading threading)
        {
            this.actionManager = actionManager;
            this.shortcutManager = shortcutManager;
            this.threading = threading;

            var buttons = new List<string>
            {
                "Options",
                "Done"
            };

            // ReSharper disable ConvertToLambdaExpression
            agent.AgentClicked.Advise(lifetime, _ =>
            {
                var lifetimeDefinition = Lifetimes.Define(lifetime);

                var options = GetOptions();

                agent.ShowBalloon(lifetimeDefinition.Lifetime, "What do you want to do?",
                    string.Empty, options, buttons, true,
                    balloonLifetime =>
                    {
                        agent.BalloonOptionClicked.Advise(balloonLifetime, tag =>
                        {
                            lifetimeDefinition.Terminate();
                            ExecuteOption(tag);
                        });

                        agent.ButtonClicked.Advise(balloonLifetime, button =>
                        {
                            lifetimeDefinition.Terminate();
                            if (button == "Options")
                                ExecuteAction("ShowOptions");
                        });
                    });
            });
            // ReSharper restore ConvertToLambdaExpression
        }

        private List<BalloonOption> GetOptions()
        {
            // TODO: Different options depending on what's selected
            // E.g. Solution/no solution, editor visible, solution explorer
            var options = new List<BalloonOption>();
            threading.ReentrancyGuard.Execute("Agent::PopulateActions", () =>
            {
                AddAction(options, "RefactorThis");
                AddAction(options, "NavigateTo");
                AddAction(options, "InspectThis");
                AddAction(options, "Generate");
                AddAction(options, "GenerateFileBesides");
                AddAction(options, "CleanupCode");
                AddAction(options, "FindUsages");
                AddAction(options, "GotoSymbol");
                AddAction(options, "GotoRecentFiles");
                AddAction(options, "GotoRecentEdits");
            });
            return options;
        }

        private void AddAction(ICollection<BalloonOption> options, string actionId)
        {
            var action = actionManager.GetExecutableAction(actionId);
            if (action == null || !actionManager.UpdateAction(action))
                return;

            var shortcutText = string.Empty;

            var shortcuts = shortcutManager.GetShortcutsWithScopes(action);
            if (shortcuts.Any())
            {
                var keyboardShortcuts = shortcuts[0].First.KeyboardShortcuts;
                if (keyboardShortcuts.Any())
                    shortcutText = string.Format(" ({0})", keyboardShortcuts[0]);
            }

            options.Add(new BalloonOption(action.Presentation.Text + shortcutText, action));
        }

        private void ExecuteOption(object tag)
        {
            var action = tag as Action;
            if (action != null)
                action();

            var executableAction = tag as IExecutableAction;
            if (executableAction != null)
                ExecuteAction(executableAction);

            var actionId = tag as string;
            if (actionId != null)
                ExecuteAction(actionId);
        }

        private void ExecuteAction(string actionId)
        {
            var action = actionManager.GetExecutableAction(actionId);
            if (action != null)
                ExecuteAction(action);
        }

        private void ExecuteAction(IExecutableAction action)
        {
            threading.ReentrancyGuard.ExecuteOrQueue(EternalLifetime.Instance, "AgentAction",
                () => actionManager.ExecuteActionIfAvailable(action));
        }
    }
}