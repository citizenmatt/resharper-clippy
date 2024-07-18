using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.ActionsMenu;
using JetBrains.UI.RichText;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public interface IOriginalActionHandler<TWorkflowProvider, TWorkflow, TActionGroup>
        where TWorkflowProvider : class, IWorkflowProvider<TWorkflow, TActionGroup>
        where TWorkflow : IWorkflow<TActionGroup>
        where TActionGroup : ExtensibleActionGroup
    {
        ICollection<TWorkflowProvider> GetWorkflowProviders();
        int CompareWorkflowItems((TWorkflow, TWorkflowProvider) item1, (TWorkflow, TWorkflowProvider) item2);
        bool IsAvailable(IDataContext context, TWorkflow workflow);
        bool IsEnabled(IDataContext context, TWorkflow workflow);
        void Execute(IDataContext context, TWorkflow workflow);
        bool ShowMenuWithOneItem { get; }
        RichText Caption { get; }
    }
}