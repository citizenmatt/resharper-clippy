using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.ActionsMenu;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public interface IOriginalActionHandler<TWorkflowProvider, TWorkflow, TActionGroup>
        where TWorkflowProvider : class, IWorkflowProvider<TWorkflow, TActionGroup>
        where TWorkflow : IWorkflow<TActionGroup>
        where TActionGroup : ActionGroup
    {
        ICollection<TWorkflowProvider> GetWorkflowProviders();
        int CompareWorkflowItems(Pair<TWorkflow, TWorkflowProvider> item1,
            Pair<TWorkflow, TWorkflowProvider> item2);
        bool IsAvailable(IDataContext context, TWorkflow workflow);
        bool IsEnabled(IDataContext context, TWorkflow workflow);
        void Execute(IDataContext context, TWorkflow workflow);
        bool ShowMenuWithOneItem { get; }
        string Caption { get; }
    }
}