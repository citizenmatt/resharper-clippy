using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.LiveTemplates.FileTemplates;
using JetBrains.UI.RichText;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<GenerateFromTemplateItemProvider, IGenerateActionWorkflow, GenerateActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<GenerateFromTemplateItemProvider, IGenerateActionWorkflow, GenerateActionGroup>;

    public class FileTemplatesGenerateAction : GenerateActionBase<GenerateFromTemplateItemProvider>, IExecutableAction, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public FileTemplatesGenerateAction(Lifetime lifetime, Agent agent, IActionManager actionManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager);
        }

        protected override RichText Caption => "Create File From Template";

        protected override ICollection<GenerateFromTemplateItemProvider> GetWorkflowProviders()
            => new List<GenerateFromTemplateItemProvider> { new(true) };


        // Oooh. That's messy.

        void IExecutableAction.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        ICollection<GenerateFromTemplateItemProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(
            (IGenerateActionWorkflow, GenerateFromTemplateItemProvider) item1,
            (IGenerateActionWorkflow, GenerateFromTemplateItemProvider) item2)
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

        RichText IOriginalActionHandler.Caption => Caption;
    }
}