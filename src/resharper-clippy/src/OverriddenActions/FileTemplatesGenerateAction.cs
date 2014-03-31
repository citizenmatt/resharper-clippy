using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.LiveTemplates.FileTemplates;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public class FileTemplatesGenerateAction : GenerateActionBase<GenerateFromTemplateItemProvider>,
        IActionHandler, IOriginalActionHandler<GenerateFromTemplateItemProvider>
    {
        private readonly AgentExtensibleAction<GenerateFromTemplateItemProvider> actionHelper;

        public FileTemplatesGenerateAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IShortcutManager shortcutManager)
        {
            actionHelper = new AgentExtensibleAction<GenerateFromTemplateItemProvider>(lifetime, this, agent,
                actionManager, shortcutManager);
        }


        // Oooh. That's messy.

        void IActionHandler.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        ICollection<GenerateFromTemplateItemProvider> IOriginalActionHandler<GenerateFromTemplateItemProvider>.GetWorkflowProviders()
        {
            return base.GetWorkflowProviders();
        }

        int IOriginalActionHandler<GenerateFromTemplateItemProvider>.CompareWorkflowItems(Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider> item1, Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider> item2)
        {
            return CompareWorkflowItems(item1, item2);
        }

        bool IOriginalActionHandler<GenerateFromTemplateItemProvider>.IsAvailable(IDataContext context, IGenerateActionWorkflow workflow)
        {
            return IsAvailable(context, workflow);
        }

        bool IOriginalActionHandler<GenerateFromTemplateItemProvider>.IsEnabled(IDataContext context, IGenerateActionWorkflow workflow)
        {
            return IsEnabled(context, workflow);
        }

        void IOriginalActionHandler<GenerateFromTemplateItemProvider>.Execute(IDataContext context, IGenerateActionWorkflow workflow)
        {
            Execute(context, workflow);
        }

        bool IOriginalActionHandler<GenerateFromTemplateItemProvider>.ShowMenuWithOneItem
        {
            get { return ShowMenuWithOneItem; }
        }
    }
}