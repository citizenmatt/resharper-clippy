using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.ContextNavigation;
using JetBrains.ReSharper.Features.Finding.NavigateFromHere;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<INavigateFromHereProvider, ContextNavigation, NavigationActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<INavigateFromHereProvider, ContextNavigation, NavigationActionGroup>;

    public class NavigateFromHereAction : ContextNavigationActionBase<INavigateFromHereProvider>,
        IActionHandler, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public NavigateFromHereAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IShortcutManager shortcutManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager, shortcutManager);
        }

        void IActionHandler.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        protected override bool ShowMenuWithOneItem
        {
            get { return true; }
        }

        protected override RichText Caption
        {
            get { return "Navigate to"; }
        }


        ICollection<INavigateFromHereProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(Pair<ContextNavigation, INavigateFromHereProvider> item1, Pair<ContextNavigation, INavigateFromHereProvider> item2)
        {
            return CompareWorkflowItems(item1, item2);
        }

        bool IOriginalActionHandler.IsAvailable(IDataContext context, ContextNavigation workflow)
        {
            return IsAvailable(context, workflow);
        }

        bool IOriginalActionHandler.IsEnabled(IDataContext context, ContextNavigation workflow)
        {
            return IsEnabled(context, workflow);
        }

        void IOriginalActionHandler.Execute(IDataContext context, ContextNavigation workflow)
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