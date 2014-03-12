using System;
using System.Collections.Generic;
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
        private readonly IThreading threading;

        public AgentClickHandler(Lifetime lifetime, Agent agent, IActionManager actionManager, IThreading threading)
        {
            this.actionManager = actionManager;
            this.threading = threading;

            var buttons = new List<string>
            {
                "Options",
                "Ok"
            };

            // TODO: Different options depending on what's selected
            // E.g. Solution/no solution, editor visible, solution explorer
            // Get DataContext from ambient selection
            var options = new List<BalloonOption>
            {
                new BalloonOption("Refactor this", "RefactorThis"),
                new BalloonOption("Navigate from here", "NavigateTo"),
                new BalloonOption("Inspect this", "InspectThis"),
                new BalloonOption("Generate code", "Generate"),
                new BalloonOption("Create new file", "GenerateFileBesides"),
                new BalloonOption("Find usages", "FindUsages"),
                new BalloonOption("Go to symbol", "GotoSymbol"),
                new BalloonOption("Clean code", "CleanupCode"),
            };

            // ReSharper disable ConvertToLambdaExpression
            agent.AgentClicked.Advise(lifetime, _ =>
            {
                var lifetimeDefinition = Lifetimes.Define(lifetime);
                agent.ShowBalloon(lifetimeDefinition.Lifetime, "What do you want to do?",
                    "(Note: Need to make list smarter based on solution open/closed, etc)",
                    options, buttons,
                    balloonLifetime =>
                    {
                        agent.BalloonOptionClicked.Advise(balloonLifetime, tag =>
                        {
                            lifetimeDefinition.Terminate();
                            var action = tag as Action;
                            if (action != null)
                                action();
                            var actionId = tag as string;
                            if (actionId != null)
                                ExecuteAction(actionId);
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

        private void ExecuteAction(string actionId)
        {
            var action = actionManager.GetExecutableAction(actionId);
            if (action != null)
            {
                threading.ReentrancyGuard.ExecuteOrQueue(EternalLifetime.Instance, "AgentAction",
                    () => actionManager.ExecuteActionIfAvailable(action));
            }
        }
    }
}