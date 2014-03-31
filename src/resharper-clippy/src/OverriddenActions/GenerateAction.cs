using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public class GenerateAction : GenerateActionBase<IGenerateActionProvider>,
        IActionHandler, IOriginalActionHandler<IGenerateActionProvider>
    {
        private readonly AgentExtensibleAction<IGenerateActionProvider> actionHelper;

        public GenerateAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IShortcutManager shortcutManager)
        {
            actionHelper = new AgentExtensibleAction<IGenerateActionProvider>(lifetime, this, agent, actionManager, shortcutManager);
        }

        void IActionHandler.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }


        ICollection<IGenerateActionProvider> IOriginalActionHandler<IGenerateActionProvider>.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler<IGenerateActionProvider>.CompareWorkflowItems(Pair<IGenerateActionWorkflow, IGenerateActionProvider> item1, Pair<IGenerateActionWorkflow, IGenerateActionProvider> item2)
        {
            return CompareWorkflowItems(item1, item2);
        }

        bool IOriginalActionHandler<IGenerateActionProvider>.IsAvailable(IDataContext context, IGenerateActionWorkflow workflow)
        {
            return IsAvailable(context, workflow);
        }

        bool IOriginalActionHandler<IGenerateActionProvider>.IsEnabled(IDataContext context, IGenerateActionWorkflow workflow)
        {
            return IsEnabled(context, workflow);
        }

        void IOriginalActionHandler<IGenerateActionProvider>.Execute(IDataContext context,
            IGenerateActionWorkflow workflow)
        {
            Execute(context, workflow);
        }

        bool IOriginalActionHandler<IGenerateActionProvider>.ShowMenuWithOneItem
        {
            get { return ShowMenuWithOneItem; }
        }
    }
}