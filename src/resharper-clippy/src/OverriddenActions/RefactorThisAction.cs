using System;
using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.UI.RichText;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<IRefactoringWorkflowProvider, IRefactoringWorkflow, RefactoringActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<IRefactoringWorkflowProvider, IRefactoringWorkflow, RefactoringActionGroup>;

    public class RefactorThisAction : IntroduceWithOccurrencesAction<IRefactoringWorkflowProvider>,
        IExecutableAction, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public RefactorThisAction(Lifetime lifetime, Agent agent, IActionManager actionManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager);
        }

        protected override RichText Caption => "Refactor This";


        void IExecutableAction.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        ICollection<IRefactoringWorkflowProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(
            ValueTuple<IRefactoringWorkflow, IRefactoringWorkflowProvider> item1,
            ValueTuple<IRefactoringWorkflow, IRefactoringWorkflowProvider> item2)
        {
            return CompareWorkflowItems(item1, item2);
        }

        bool IOriginalActionHandler.IsAvailable(IDataContext context, IRefactoringWorkflow workflow)
        {
            return IsAvailable(context, workflow);
        }

        bool IOriginalActionHandler.IsEnabled(IDataContext context, IRefactoringWorkflow workflow)
        {
            return IsEnabled(context, workflow);
        }

        void IOriginalActionHandler.Execute(IDataContext context, IRefactoringWorkflow workflow)
        {
            Execute(context, workflow);
        }

        bool IOriginalActionHandler.ShowMenuWithOneItem => ShowMenuWithOneItem;

        RichText IOriginalActionHandler.Caption => Caption;
    }
}