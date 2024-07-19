using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.InplaceRefactorings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Threading;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class InplaceRefactoringHandler(Lifetime lifetime,
                                           Agent agent,
                                           InplaceRefactoringsManager inplaceRefactoringsManager,
                                           IPsiFiles psiFiles,
                                           IThreading threading,
                                           IActionManager actionManager,
                                           ISolution solution)
        : IHighlightingChangeHandler
    {
        private readonly SequentialLifetimes balloonLifetimes = new(lifetime);
        // private readonly SequentialLifetimes highlighterLifetimes = new(lifetime);

        private IHighlighter currentHighlighter;
        // private bool showingBalloon;
        private LifetimeDefinition scheduledRemovalLifetimeDefinition;

        public void OnHighlightingChanged(IDocument document, ICollection<IHighlighter> added, ICollection<IHighlighter> removed, ICollection<IHighlighter> modified)
        {
            // Sadly, we're not batched. The highlight gets removed, and then added again.
            // First time the highlight is added, show the advice
            // When the highlight is updated, it is first removed a new one is immediately added
            // -> Enqueue the removal. Return
            // -> Check to see if it's a "different" highlight. If not, cancel the removal
            // -> If it is, cancel the removal, and show the advice
            var sourceFile = document.GetPsiSourceFile(solution);
            var highlighter = added.FirstOrDefault(IsInplaceRefactoringHighlight);
            if (sourceFile != null && highlighter != null)
            {
                if (IsNewHighlighter(highlighter))
                {
                    // var newHighlightLifetime = highlighterLifetimes.Next();
                    // newHighlightLifetime.OnTermination(() => currentHighlighter = null);
                    // currentHighlighter = highlighter;

                    IRefactoringInfo refactoringInfo;
                    using (CompilationContextCookie.GetOrCreate(UniversalModuleReferenceContext.Instance))
                    using (ReadLockCookie.Create())
                    {
                        psiFiles.CommitAllDocuments();
                        refactoringInfo = inplaceRefactoringsManager.GetRefactoringAvailable(sourceFile, highlighter.Range.StartOffset);
                    }

                    if (refactoringInfo == null)
                    {
                        // Throws a bunch of seemingly unrelated exceptions. No idea why
                        // if (highlighter is HighlighterOnRangeMarker highlighterOnRangeMarker)
                        // {
                        //     void RangeMarkerOnChanged(object sender, RangeMarkerChangedEventArgs e)
                        //     {
                        //         if (showingBalloon) return;
                        //
                        //         IRefactoringInfo info;
                        //         using (CompilationContextCookie.GetOrCreate(UniversalModuleReferenceContext.Instance))
                        //         using (ReadLockCookie.Create())
                        //         {
                        //             psiFiles.CommitAllDocuments();
                        //             info = inplaceRefactoringsManager.GetRefactoringAvailable(sourceFile,
                        //                 e.NewRange.StartOffset);
                        //         }
                        //
                        //         if (info != null)
                        //         {
                        //             CancelHideBalloon();
                        //             ShowRefactoringAdvice(highlighter, info);
                        //         }
                        //     }
                        //
                        //     highlighterOnRangeMarker.RangeMarker.Changed += RangeMarkerOnChanged;
                        //     newHighlightLifetime.OnTermination(() => highlighterOnRangeMarker.RangeMarker.Changed -= RangeMarkerOnChanged);
                        // }
                        
                        return;
                    }

                    CancelHideBalloon();
                    ShowRefactoringAdvice(highlighter, refactoringInfo);
                }
                else
                {
                    CancelHideBalloon();
                }
            }

            highlighter = removed.FirstOrDefault(IsInplaceRefactoringHighlight);
            if (highlighter != null)
            {
                ScheduleHideBalloon();
                // highlighterLifetimes.TerminateCurrent();
            }
        }

        private static bool IsInplaceRefactoringHighlight(IHighlighter highlighter)
        {
            return highlighter.AttributeId == InplaceRefactoringsHighlightingManager.ENTITY_EDITED_ATTRIBUTE_ID;
        }

        private void ScheduleHideBalloon()
        {
            scheduledRemovalLifetimeDefinition = lifetime.CreateNested();

            threading.ReentrancyGuard.ExecuteOrQueue(scheduledRemovalLifetimeDefinition.Lifetime,
                "Clippy::InplaceRefactoringHandler::DeferredHideBalloon",
                () =>
                {
                    balloonLifetimes.TerminateCurrent();
                    currentHighlighter = null;
                });
        }

        private void CancelHideBalloon()
        {
            scheduledRemovalLifetimeDefinition?.Terminate();
        }

        private bool IsNewHighlighter(IHighlighter highlighter)
        {
            if (currentHighlighter == null)
                return true;

            if (highlighter == currentHighlighter)
                return false;

            return !highlighter.Range.StrictIntersects(currentHighlighter.Range);
        }

        private void ShowRefactoringAdvice(IHighlighter highlighter, IRefactoringInfo refactoringInfo)
        {
            balloonLifetimes.Next(refactoringLifetime =>
            {
                var message = GetMessage();
                var options = GetOptions(refactoringInfo);
                agent.ShowBalloon(refactoringLifetime, string.Empty, message, options, ["Cancel"], false,
                    balloonLifetime =>
                    {
                        // showingBalloon = true;
                        // balloonLifetime.OnTermination(() => showingBalloon = false);

                        currentHighlighter = highlighter;

                        agent.ButtonClicked.Advise(balloonLifetime, _ => balloonLifetimes.TerminateCurrent());
                        agent.BalloonOptionClicked.Advise(balloonLifetime, tag =>
                        {
                            // Terminate first. This kills the balloon window before we start
                            // the refactoring UI. If we start the refactoring UI first, I think
                            // it picks up the balloon window as its parent, and it closes when
                            // the balloon closes
                            balloonLifetimes.TerminateCurrent();

                            if (tag is Action action)
                                action();
                        });
                    });
            });
        }

        private static string GetMessage()
        {
            return "It looks like you're refactoring code." + Environment.NewLine + Environment.NewLine +
                   "Would you like help?";
        }

        private IList<BalloonOption> GetOptions(IRefactoringInfo refactoringInfo)
        {
            var options = new List<BalloonOption>
            {
                new(refactoringInfo.ContextActionTitle, (Action)ApplyRefactoringAction),
                new("Just edit the code without help")
            };
            return options;

            void ApplyRefactoringAction()
            {
                ReadLockCookie.GuardedExecute(() =>
                {
                    Lifetime.Using(l =>
                    {
                        // I think this will fail if the cursor moves out of the refactoring range
                        var dataRules = DataRules.AddRule("DoInplaceRefactoringContextActionBase", ProjectModelDataConstants.SOLUTION, solution);
                        var dataContext = actionManager.DataContexts.CreateOnSelection(l, dataRules);
                        RefactoringActionUtil.ExecuteRefactoring(dataContext, refactoringInfo.CreateRefactoringWorkflow());
                    });
                });
            }
        }
    }
}