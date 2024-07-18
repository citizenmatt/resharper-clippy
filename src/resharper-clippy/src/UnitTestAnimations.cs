using System;
using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Execution;
using JetBrains.ReSharper.UnitTestFramework.Execution.Launch;
using JetBrains.ReSharper.UnitTestFramework.Session;
using JetBrains.ReSharper.UnitTestFramework.UI.Session;
using JetBrains.Threading;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class UnitTestAnimations
    {
        private readonly IUnitTestResultManager resultsManager;
        private readonly Agent agent;
        private readonly IThreading threading;

        public UnitTestAnimations(Lifetime lifetime, IUnitTestSessionConductor sessionConductor,
            IUnitTestResultManager resultsManager, Agent agent, IThreading threading)
        {
            this.resultsManager = resultsManager;
            this.agent = agent;
            this.threading = threading;
            var testSessionLifetimes = new Dictionary<IUnitTestSessionTreeViewModel, LifetimeDefinition>();

            sessionConductor.SessionOpened.Advise(lifetime, sessionView =>
            {
                var sessionLifetimeDefinition = lifetime.CreateNested();
                testSessionLifetimes.Add(sessionView, sessionLifetimeDefinition);

                SubscribeToSessionLaunch(sessionLifetimeDefinition.Lifetime, sessionView.Session);
            });

            sessionConductor.SessionClosed.Advise(lifetime, sessionView =>
            {
                if (testSessionLifetimes.TryGetValue(sessionView, out var sessionLifetimeDefinition))
                {
                    sessionLifetimeDefinition.Terminate();
                    testSessionLifetimes.Remove(sessionView);
                }
            });
        }

        private void SubscribeToSessionLaunch(Lifetime sessionLifetime, IUnitTestSession session)
        {
            var sequentialLifetimes = new SequentialLifetimes(sessionLifetime);

            session.Launch.Change.Advise(sessionLifetime, args =>
            {
                if (args.HasNew && args.New != null)
                {
                    sequentialLifetimes.Next(launchLifetime =>
                    {
                        SubscribeToLaunchState(launchLifetime, session, args.New.StageStatus);
                    });
                }
            });
        }

        private void SubscribeToLaunchState(Lifetime launchLifetime, IUnitTestSession session,
            IProperty<UnitTestLaunchStageStatus> state)
        {
            var aborted = false;
            state.Change.Advise(launchLifetime, stateArgs =>
            {
                if (stateArgs.HasNew)
                {
                    switch (stateArgs.New)
                    {
                        case UnitTestLaunchStageStatus.None:
                        case UnitTestLaunchStageStatus.Running:
                            break;

                        case UnitTestLaunchStageStatus.Building:
                            aborted = false;
                            threading.Dispatcher.BeginOrInvoke("Clippy::TestBuilding", () => agent.Play("Processing"));
                            break;

                        case UnitTestLaunchStageStatus.Starting:
                            if (!aborted)
                                threading.Dispatcher.BeginOrInvoke("Clippy::TestStarting", () => agent.Play("GetTechy"));
                            break;

                        case UnitTestLaunchStageStatus.BuildFailed:
                            aborted = true;
                            threading.Dispatcher.BeginOrInvoke("Clippy::TestAborting", () => agent.Play("Wave"));
                            break;

                        case UnitTestLaunchStageStatus.Finishing:
                            if (!aborted)
                            {
                                threading.Dispatcher.BeginOrInvoke("Clippy::TestStopping", () =>
                                {
                                    string message = null;
                                    var animation = "Congratulate";
                                    ReadLockCookie.GuardedExecute(() =>
                                    {
                                        var results = resultsManager.GetResults(session.Elements, session);
                                        message = GetStatusMessage(results, out var success);
                                        if (!success)
                                            animation = "Wave";
                                    });

                                    var balloonLifetimeDefinition = launchLifetime.CreateNested();

                                    agent.ShowBalloon(balloonLifetimeDefinition.Lifetime, "Test run complete",
                                        message, null, new[] { "Done" }, false,
                                        balloonLifetime =>
                                        {
                                            threading.TimedActions.Queue(balloonLifetimeDefinition.Lifetime,
                                                "Clippy::Balloon", balloonLifetimeDefinition.Terminate,
                                                TimeSpan.FromSeconds(5), TimedActionsHost.Recurrence.OneTime,
                                                Rgc.Guarded);
                                            agent.ButtonClicked.Advise(balloonLifetime, balloonLifetimeDefinition.Terminate);
                                        });
                                    agent.Play(animation);
                                });
                            }
                            break;
                    }
                }
            });
        }

        private static string GetStatusMessage(IEnumerable<KeyValuePair<IUnitTestElement, UnitTestResult>> results,
            out bool success)
        {
            GetStatusCounts(results,
                out var successCount,
                out var failCount,
                out var ignoreCount,
                out var inconclusiveCount,
                false);

            success = failCount <= 0;

            return
                $"Tests passed: {successCount}, failed {failCount}, ignored {ignoreCount}, inconclusive {inconclusiveCount}";
        }

        private static void GetStatusCounts(IEnumerable<KeyValuePair<IUnitTestElement, UnitTestResult>> results,
            out int successCount, out int failCount, out int ignoreCount, out int inconclusiveCount, bool countContainers)
        {
            successCount = 0;
            failCount = 0;
            ignoreCount = 0;
            inconclusiveCount = 0;

            foreach (var pair in results)
            {
                if (pair.Key.Children.Count == 0 || countContainers)
                {
                    if (pair.Value.Status.Has(UnitTestStatus.Success))
                        successCount++;
                    else if (pair.Value.Status.Has(UnitTestStatus.Failed))
                        failCount++;
                    else if (pair.Value.Status.Has(UnitTestStatus.Ignored))
                        ignoreCount++;
                    else if (pair.Value.Status.Has(UnitTestStatus.Inconclusive))
                        inconclusiveCount++;
                }
            }
        }
    }
}