using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.LiveTemplates.FileTemplates;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<GenerateFromTemplateItemProvider, IGenerateActionWorkflow, GenerateActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<GenerateFromTemplateItemProvider, IGenerateActionWorkflow, GenerateActionGroup>;

    public class FileTemplatesGenerateAction : GenerateActionBase<GenerateFromTemplateItemProvider>,
        IActionHandler, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public FileTemplatesGenerateAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IShortcutManager shortcutManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager, shortcutManager);
        }

        protected override RichText Caption
        {
            get { return "Create File From Template"; }
        }


        // Oooh. That's messy.

        void IActionHandler.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        ICollection<GenerateFromTemplateItemProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return base.GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider> item1, Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider> item2)
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

        bool IOriginalActionHandler.ShowMenuWithOneItem
        {
            get { return ShowMenuWithOneItem; }
        }

        string IOriginalActionHandler.Caption { get { return Caption; } }
    }
}