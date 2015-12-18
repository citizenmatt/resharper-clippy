using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.UI.ActionsRevised;
using JetBrains.Util;

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

        protected override bool ShowMenuWithOneItem
        {
            get { return true; }
        }


        ICollection<IGenerateWorkflowProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(Pair<IGenerateActionWorkflow, IGenerateWorkflowProvider> item1, Pair<IGenerateActionWorkflow, IGenerateWorkflowProvider> item2)
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

        void IOriginalActionHandler.Execute(IDataContext context,
            IGenerateActionWorkflow workflow)
        {
            Execute(context, workflow);
        }

        bool IOriginalActionHandler.ShowMenuWithOneItem
        {
            get { return ShowMenuWithOneItem; }
        }

        string IOriginalActionHandler.Caption { get { return base.Caption; } }
    }
}