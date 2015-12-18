using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.UI.ActionsRevised;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<IRefactoringWorkflowProvider, IRefactoringWorkflow, RefactoringActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<IRefactoringWorkflowProvider, IRefactoringWorkflow, RefactoringActionGroup>;

    public class RefactorThisAction : IntroduceWithOccurencesAction<IRefactoringWorkflowProvider>,
        IExecutableAction, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public RefactorThisAction(Lifetime lifetime, Agent agent, IActionManager actionManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager);
        }

        protected override RichText Caption
        {
            get { return "Refactor This"; }
        }


        void IExecutableAction.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        ICollection<IRefactoringWorkflowProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(Pair<IRefactoringWorkflow, IRefactoringWorkflowProvider> item1, Pair<IRefactoringWorkflow, IRefactoringWorkflowProvider> item2)
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

        bool IOriginalActionHandler.ShowMenuWithOneItem
        {
            get { return ShowMenuWithOneItem; }
        }

        string IOriginalActionHandler.Caption { get { return Caption; } }
    }
}