using System;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.VsIntegration.Shell.EnvDte;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class SaveAnimations
    {
        public SaveAnimations(Lifetime lifetime, Agent agent, IThreading threading, IEnvDteWrapper envDteWrapper)
        {
            var saveEvent = threading.CreateGroupingEvent(lifetime, "Clippy::Save", TimeSpan.FromSeconds(1),
                () => agent.Play("Save"));
            var documentEvents = envDteWrapper.Events.TryGetDocumentEvents(lifetime);
            documentEvents?.DocumentSaved.Advise(lifetime, _ => saveEvent.FireIncoming());
        }
    }
}