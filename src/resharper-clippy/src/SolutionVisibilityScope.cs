using System;
using JetBrains.ProjectModel;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    [SolutionComponent]
    public class SolutionVisibilityScope : IDisposable
    {
        private readonly Agent agent;

        public SolutionVisibilityScope(Agent agent)
        {
            this.agent = agent;

            agent.Show(true);
        }

        public void Dispose()
        {
            agent.Hide(true);
        }
    }
}