using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.VsIntegration.Shell.EnvDte;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class BuildAnimations
    {
        public BuildAnimations(Lifetime lifetime, Agent agent, IEnvDteWrapper envDteWrapper)
        {
            var sequentialLifetimes = new SequentialLifetimes(lifetime);

            var buildEvents = envDteWrapper.Events.BuildEvents;
            buildEvents.OnBuildBegin.Advise(lifetime, _ => sequentialLifetimes.Next(buildLifetime => agent.Play(buildLifetime, "Processing")));
            buildEvents.OnBuildDone.Advise(lifetime, _ => sequentialLifetimes.TerminateCurrent());
        }
    }
}