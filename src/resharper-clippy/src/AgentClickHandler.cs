using System;
using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Lifetimes;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [ShellComponent]
    public class AgentClickHandler
    {
        private readonly IActionManager actionManager;
        private readonly IThreading threading;

        public AgentClickHandler(Lifetime lifetime, Agent agent, IActionManager actionManager, IThreading threading)
        {
            this.actionManager = actionManager;
            this.threading = threading;

            var buttons = new List<string>
            {
                "Options",
                "Done"
            };

            agent.AgentClicked.Advise(lifetime, _ =>
            {
                var lifetimeDefinition = lifetime.CreateNested();

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
            var action = actionManager.Defs.TryGetActionDefById(actionId);
            if (action == null || !actionManager.Handlers.StaticEvaluate(action).IsAvailable)
                return;

            var shortcutText = actionManager.PresentableTexts.GetShortcutText(action);
            if (!string.IsNullOrEmpty(shortcutText))
                shortcutText = $" ({shortcutText})";

            options.Add(new BalloonOption(action.Text + shortcutText, actionId));
        }

        private void ExecuteOption(object tag)
        {
            if (tag is Action action)
                action();

            if (tag is string actionId)
                ExecuteAction(actionId);
        }

        private void ExecuteAction(string actionId)
        {
            var action = actionManager.Defs.TryGetActionDefById(actionId);
            if (action != null)
            {
                threading.ReentrancyGuard.ExecuteOrQueue("Agent::ClickAction", () => action.EvaluateAndExecute(actionManager));
            }
        }
    }
}