using System;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using EnvDTE;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class SaveAnimations
    {
        private readonly Lifetime lifetime;
        private readonly DTE dte;
        private readonly GroupingEvent saveEvent;

        public SaveAnimations(Lifetime lifetime, Agent agent, IThreading threading)
        {
            this.lifetime = lifetime;

            dte = Shell.Instance.GetComponent<DTE>();

            saveEvent = threading.CreateGroupingEvent(lifetime, "Clippy::Save", TimeSpan.FromSeconds(1), () => agent.Play("Save"));

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            var events = dte.Events;
            if (events == null)
                return;

            var documentEvents = events.DocumentEvents;
            if (documentEvents == null)
                return;

            lifetime.AddBracket(() =>
            {
                documentEvents.DocumentSaved += OnDocumentSaved;
            }, () =>
            {
                documentEvents.DocumentSaved -= OnDocumentSaved;
            });
        }

        void OnDocumentSaved(Document document)
        {
            saveEvent.FireIncoming();
        }
    }
}