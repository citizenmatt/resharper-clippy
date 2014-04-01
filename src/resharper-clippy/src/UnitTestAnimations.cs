using System;
using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Threading;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class UnitTestAnimations
    {
        private readonly IUnitTestResultManager resultsManager;
        private readonly Agent agent;
        private readonly IThreading threading;

        public UnitTestAnimations(Lifetime lifetime, IUnitTestSessionManager sessionManager,
            IUnitTestResultManager resultsManager, Agent agent, IThreading threading)
        {
            this.resultsManager = resultsManager;
            this.agent = agent;
            this.threading = threading;
            var testSessionLifetimes = new Dictionary<IUnitTestSessionView, LifetimeDefinition>();

            sessionManager.SessionCreated.Advise(lifetime, sessionView =>
            {
                var sessionLifetimeDefinition = Lifetimes.Define(lifetime, "Clippy::TestSession");
                testSessionLifetimes.Add(sessionView, sessionLifetimeDefinition);

                SubscribeToSessionLaunch(sessionLifetimeDefinition.Lifetime, sessionView.Session);
            });

            sessionManager.SessionClosed.Advise(lifetime, sessionView =>
            {
                LifetimeDefinition sessionLifetimeDefinition;
                if (testSessionLifetimes.TryGetValue(sessionView, out sessionLifetimeDefinition))
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
                        SubscribeToLaunchState(launchLifetime, session, args.New.State);
                    });
                }
            });
        }

        private void SubscribeToLaunchState(Lifetime launchLifetime, IUnitTestSession session,
            IProperty<UnitTestSessionState> state)
        {
            var aborted = false;
            state.Change.Advise(launchLifetime, stateArgs =>
            {
                if (stateArgs.HasNew)
                {
                    switch (stateArgs.New)
                    {
                        case UnitTestSessionState.Idle:
                        case UnitTestSessionState.Running:
                            break;

                        case UnitTestSessionState.Building:
                            aborted = false;
                            threading.Dispatcher.BeginOrInvoke("Clippy::TestBuilding", () => agent.Play("Processing"));
                            break;

                        case UnitTestSessionState.Starting:
                            if (!aborted)
                                threading.Dispatcher.BeginOrInvoke("Clippy::TestStarting", () => agent.Play("GetTechy"));
                            break;

                        case UnitTestSessionState.Stopping:
                            if (!aborted)
                            {
                                threading.Dispatcher.BeginOrInvoke("Clippy::TestStopping", () =>
                                {
                                    string message = null;
                                    var animation = "Congratulate";
                                    ReadLockCookie.GuardedExecute(() =>
                                    {
                                        var results = resultsManager.GetResults(session.Elements);
                                        bool success;
                                        message = GetStatusMessage(results, out success);
                                        if (!success)
                                            animation = "Wave";
                                    });

                                    var balloonLifetimeDefinition = Lifetimes.Define(launchLifetime);

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

                        case UnitTestSessionState.Aborting:
                            aborted = true;
                            threading.Dispatcher.BeginOrInvoke("Clippy::TestAborting", () => agent.Play("Wave"));
                            break;
                    }
                }
            });
        }

        private static string GetStatusMessage(IEnumerable<KeyValuePair<IUnitTestElement, UnitTestResult>> results,
            out bool success)
        {
            int successCount;
            int failCount;
            int ignoreCount;
            int inconclusiveCount;
            GetStatusCounts(results, out successCount, out failCount, out ignoreCount, out inconclusiveCount, false);

            success = failCount <= 0;

            return string.Format("Tests passed: {0}, failed {1}, ignored {2}, inconclusive {3}", 
                successCount, failCount, ignoreCount, inconclusiveCount);
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
                if (!pair.Key.State.IsValid())
                    continue;

                if (pair.Key.Children.Count == 0 || countContainers)
                    switch (pair.Value.Status)
                    {
                        case UnitTestStatus.Unknown:
                            break;
                        case UnitTestStatus.Success:
                            successCount++;
                            break;
                        case UnitTestStatus.Failed:
                            failCount++;
                            break;
                        case UnitTestStatus.Ignored:
                            ignoreCount++;
                            break;
                        case UnitTestStatus.Aborted:
                            break;
                        case UnitTestStatus.Inconclusive:
                            inconclusiveCount++;
                            break;
                    }
            }
        }
    }
}