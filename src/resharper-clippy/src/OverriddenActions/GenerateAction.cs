using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.UI.RichText;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<IGenerateWorkflowProvider, IGenerateActionWorkflow, GenerateActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<IGenerateWorkflowProvider, IGenerateActionWorkflow, GenerateActionGroup>;

    public class GenerateAction : GenerateActionBase<IGenerateWorkflowProvider>,
        IExecutableAction, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public GenerateAction(Lifetime lifetime, Agent agent, IActionManager actionManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager);
        }

        void IExecutableAction.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        protected override bool ShowMenuWithOneItem => true;


        ICollection<IGenerateWorkflowProvider> IOriginalActionHandler.GetWorkflowProviders() => GetWorkflowProviders();

        int IOriginalActionHandler.CompareWorkflowItems(
            (IGenerateActionWorkflow, IGenerateWorkflowProvider) item1,
            (IGenerateActionWorkflow, IGenerateWorkflowProvider) item2)
        {
            return CompareWorkflowItems(item1, item2);
        }

        bool IOriginalActionHandler.IsAvailable(IDataContext context, IGenerateActionWorkflow workflow)
        {
            return IsAvailable(context, workflow);
        }

        bool IOriginalActionHandler.IsEnabled(IDataContext context, IGenerateActionWorkflow workflow)
        {
            return IsEnabled(context, workflow);
        }

        void IOriginalActionHandler.Execute(IDataContext context, IGenerateActionWorkflow workflow)
        {
            Execute(context, workflow);
        }

        bool IOriginalActionHandler.ShowMenuWithOneItem => ShowMenuWithOneItem;

        RichText IOriginalActionHandler.Caption => base.Caption;
    }
}