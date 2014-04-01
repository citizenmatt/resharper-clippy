using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using EnvDTE;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class BuildAnimations
    {
        private readonly Lifetime lifetime;
        private readonly DTE dte;
        private readonly Agent agent;
        private readonly SequentialLifetimes sequentialLifetimes;

        public BuildAnimations(Lifetime lifetime, Agent agent)
        {
            this.lifetime = lifetime;
            dte = Shell.Instance.GetComponent<DTE>();
            this.agent = agent;

            sequentialLifetimes = new SequentialLifetimes(lifetime);

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            var events = dte.Events;
            if (events == null)
                return;

            var buildEvents = events.BuildEvents;
            if (buildEvents == null)
                return;

            lifetime.AddBracket(() =>
            {
                buildEvents.OnBuildBegin += OnBuildBegin;
            }, () =>
            {
                buildEvents.OnBuildBegin -= OnBuildBegin;
            });

            lifetime.AddBracket(() =>
            {
                buildEvents.OnBuildDone += OnBuildDone;
            }, () =>
            {
                buildEvents.OnBuildDone -= OnBuildDone;
            });
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            sequentialLifetimes.Next(buildLifetime => agent.Play(buildLifetime, "Processing"));
        }

        private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            sequentialLifetimes.TerminateCurrent();
        }
    }
}