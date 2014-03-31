using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public interface IOriginalActionHandler<TGenerateActionProvider>
    {
        ICollection<TGenerateActionProvider> GetWorkflowProviders();
        int CompareWorkflowItems(Pair<IGenerateActionWorkflow, TGenerateActionProvider> item1,
            Pair<IGenerateActionWorkflow, TGenerateActionProvider> item2);
        bool IsAvailable(IDataContext context, IGenerateActionWorkflow workflow);
        bool IsEnabled(IDataContext context, IGenerateActionWorkflow workflow);
        void Execute(IDataContext context, IGenerateActionWorkflow workflow);
        bool ShowMenuWithOneItem { get; }
    }
}