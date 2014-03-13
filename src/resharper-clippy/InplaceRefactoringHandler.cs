using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Refactorings.Workflow;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Threading;
using DataConstants = JetBrains.ProjectModel.DataContext.DataConstants;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [SolutionComponent]
    public class InplaceRefactoringHandler : IHighlightingChangeHandler
    {
        private readonly SequentialLifetimes sequentialLifetimes;
        private readonly Lifetime lifetime;
        private readonly Agent agent;
        private readonly InplaceRefactoringsHighlightingManagerWrapper highlightingManager;
        private readonly IThreading threading;
        private readonly IActionManager actionManager;
        private readonly ISolution solution;

        private IHighlighter currentHighlighter;
        private LifetimeDefinition deferredRemovalLifetimeDefinition;

        public InplaceRefactoringHandler(Lifetime lifetime, Agent agent,
            InplaceRefactoringsHighlightingManagerWrapper highlightingManager,
            IThreading threading, IActionManager actionManager, ISolution solution)
        {
            this.lifetime = lifetime;
            this.agent = agent;
            this.highlightingManager = highlightingManager;
            this.threading = threading;
            this.actionManager = actionManager;
            this.solution = solution;

            sequentialLifetimes = new SequentialLifetimes(lifetime);
        }

        public void OnHighlightingChanged(IDocument document, ICollection<IHighlighter> added, ICollection<IHighlighter> removed, ICollection<IHighlighter> modified)
        {
            // Sadly, we're not batched. The highlight gets removed, and then added again.
            // First time the highlight is added, show the advice
            // When the highlight is updated, it is first removed a new one is immediately added
            // -> Enqueue the removal. Return
            // -> Check to see if it's a "different" highlight. If not, cancel the removal
            // -> If it is, cancel the removal, and show the advice
            var highlighter = added.FirstOrDefault(IsInplaceRefactoringHighlight);
            if (highlighter != null)
            {
                if (IsNewHighlighter(highlighter))
                {
                    var refactoringType = highlightingManager.GetInplaceRefactoringType(document,
                        highlighter.Range.StartOffset);
                    if (refactoringType.Type == InplaceRefactoringType.None)
                        return;

                    CancelHideBalloon();
                    ShowRefactoringAdvice(highlighter, refactoringType);
                }
                else
                {
                    CancelHideBalloon();
                }
            }

            highlighter = removed.FirstOrDefault(IsInplaceRefactoringHighlight);
            if (highlighter != null)
                DeferHideBalloon();
        }

        private static bool IsInplaceRefactoringHighlight(IHighlighter highlighter)
        {
            return highlighter.AttributeId == "ReSharper Name or Signature Changed";
        }

        private void DeferHideBalloon()
        {
            deferredRemovalLifetimeDefinition = Lifetimes.Define(lifetime);

            threading.ReentrancyGuard.ExecuteOrQueue(deferredRemovalLifetimeDefinition.Lifetime, 
                "Clippy::InplaceRefactoringHandler::DeferredHideBalloon",
                () =>
                {
                    sequentialLifetimes.TerminateCurrent();
                    currentHighlighter = null;
                });
        }

        private void CancelHideBalloon()
        {
            if (deferredRemovalLifetimeDefinition != null)
                deferredRemovalLifetimeDefinition.Terminate();
        }

        private bool IsNewHighlighter(IHighlighter highlighter)
        {
            if (currentHighlighter == null)
                return true;

            if (highlighter == currentHighlighter)
                return false;

            return !highlighter.Range.Intersects(currentHighlighter.Range);
        }

        private void ShowRefactoringAdvice(IHighlighter highlighter, InplaceRefactoringInfo refactoringInfo)
        {
            sequentialLifetimes.Next(refactoringLifetime =>
            {
                var message = GetMessage();
                var options = GetOptions(refactoringInfo);
                agent.ShowBalloon(refactoringLifetime, string.Empty, message, options, new[] { "Cancel" },
                    balloonLifetime =>
                    {
                        currentHighlighter = highlighter;

                        agent.ButtonClicked.Advise(balloonLifetime, _ => sequentialLifetimes.TerminateCurrent());
                        agent.BalloonOptionClicked.Advise(balloonLifetime, tag =>
                        {
                            // Terminate first. This kills the balloon window before we start
                            // the refactoring UI. If we start the refactoring UI first, I think
                            // it picks up the balloon window as its parent, and it closes when
                            // the balloon closes
                            sequentialLifetimes.TerminateCurrent();

                            var action = tag as Action;
                            if (action != null)
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

        private IList<BalloonOption> GetOptions(InplaceRefactoringInfo refactoringInfo)
        {
            var applyRefactoringMessage = "Apply refactoring";
            switch (refactoringInfo.Type)
            {
                case InplaceRefactoringType.Rename:
                    applyRefactoringMessage = "Apply rename refactoring";
                    break;
                case InplaceRefactoringType.ChangeSignature:
                    applyRefactoringMessage = "Apply change signature refactoring";
                    break;
                case InplaceRefactoringType.MoveStaticMembers:
                    applyRefactoringMessage = "Apply move static members refactoring";
                    break;
            }

            // ReSharper disable ConvertToLambdaExpression
            Action applyRefactoringAction = () =>
            {
                ReadLockCookie.GuardedExecute(() =>
                {
                    Lifetimes.Using(l =>
                    {
                        // I think this will fail if the cursor moves out of the refactoring range
                        var dataRules = DataRules.AddRule("DoInplaceRefactoringContextActionBase",
                            DataConstants.SOLUTION, solution);
                        var dataContext = actionManager.DataContexts.CreateOnSelection(l, dataRules);
                        RefactoringActionUtil.ExecuteRefactoring(dataContext,
                            refactoringInfo.CreateRefactoringWorkflow());
                    });
                });
            };
            // ReSharper restore ConvertToLambdaExpression

            var options = new List<BalloonOption>
            {
                new BalloonOption(applyRefactoringMessage, applyRefactoringAction),
                new BalloonOption("Just edit the code without help")
            };
            return options;
        }
    }
}