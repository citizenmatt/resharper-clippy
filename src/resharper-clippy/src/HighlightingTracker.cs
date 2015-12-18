using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    public interface IHighlightingChangeHandler
    {
        void OnHighlightingChanged(IDocument document, ICollection<IHighlighter> added, ICollection<IHighlighter> removed, ICollection<IHighlighter> modified);
    }

    [SolutionComponent]
    public class HighlightingTracker
    {
        public HighlightingTracker(Lifetime lifetime, ITextControlManager textControlManager,
            IDocumentMarkupManager markupManager, IViewable<IHighlightingChangeHandler> handlers)
        {
            textControlManager.TextControls.View(lifetime, (textControlLifetime, textControl) =>
            {
                var markupModel = markupManager.GetMarkupModel(textControl.Document);

                // ReSharper disable ConvertToLambdaExpression
                Action<DocumentMarkupModifiedEventArgs> onChanged = args =>
                {
                    Lifetimes.Using(l =>
                    {
                        handlers.View(l, (_, h) =>
                        {
                            h.OnHighlightingChanged(textControl.Document, args.Added, args.Removed, args.Modified);
                        });
                    });
                };
                // ReSharper restore ConvertToLambdaExpression

                markupModel.Changed += onChanged;
                textControlLifetime.AddAction(() => markupModel.Changed -= onChanged);
            });
        }
    }

    //[SolutionComponent]
//    public class CodingToGreenHandler : IHighlightingChangeHandler
//    {
//        private static readonly Key<Boxed<DateTime>> LastGreenTimestampKey = new Key<Boxed<DateTime>>("CodingToGreenHandler::LastGreenTimestamp");
//        private static readonly TimeSpan IgnoreInterval = TimeSpan.FromSeconds(3);
//
//        private readonly IDocumentMarkupManager markupManager;
//        private readonly WeakCollection<IDocument> modifiedDocuments;
//        private readonly GroupingEvent groupingEvent;
//
//        public CodingToGreenHandler(Lifetime lifetime, Agent agent, IDocumentMarkupManager markupManager, IThreading threading)
//        {
//            this.markupManager = markupManager;
//
//            modifiedDocuments = new WeakCollection<IDocument>();
//
//            groupingEvent = threading.CreateGroupingEvent(lifetime, "Clippy::CodingToGreenHandler", TimeSpan.FromMilliseconds(500), () =>
//            {
//                var utcNow = DateTime.UtcNow;
//
//                using(ReadLockCookie.Create())
//                lock(modifiedDocuments)
//                {
//                    var play = false;
//                    foreach (var document in modifiedDocuments.Where(IsDocumentGreen))
//                    {
//                        if (!markupManager.GetMarkupModel(document).GetHighlightersEnumerable().Any())
//                        {
//                            var lastGreenTimestamp = document.GetData(LastGreenTimestampKey);
//                            if (lastGreenTimestamp == null || lastGreenTimestamp.Value < utcNow.Add(-IgnoreInterval))
//                                play = true;
//                            document.PutData(LastGreenTimestampKey, new Boxed<DateTime>(utcNow));
//                        }
//                    }
//
//                    if (play)
//                        agent.Play("Congratulate");
//
//                    modifiedDocuments.Clear();
//                }
//            });
//        }
//
//        private bool IsDocumentGreen(IDocument arg)
//        {
//            return markupManager.GetMarkupModel(arg).GetHighlightersEnumerable().IsEmpty();
//        }
//
//        public void OnHighlightingChanged(IDocument document, ICollection<IHighlighter> added, ICollection<IHighlighter> removed, ICollection<IHighlighter> modified)
//        {
//            if (added.Count == 0 && modified.Count == 0 && removed.Count > 0)
//            {
//                if (!markupManager.GetMarkupModel(document).GetHighlightersEnumerable().Any())
//                {
//                    lock (modified)
//                    {
//                        modifiedDocuments.Add(document);
//                        groupingEvent.FireIncoming();
//                    }
//                }
//            }
//        }
//    }
}