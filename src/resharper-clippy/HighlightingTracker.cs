using System;
using System.Collections.Generic;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    // TODO: Daemon.DaemonStageChanged2?
    // TODO: Paste on import? See ImportsForPastedContextAction
    // TODO: Add references popup?


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
}