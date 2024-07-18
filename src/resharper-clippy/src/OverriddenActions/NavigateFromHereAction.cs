using System;
using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.UI.RichText;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    using ExtensibleActionHelper = AgentExtensibleAction<INavigateFromHereProvider, ContextNavigation, NavigationActionGroup>;
    using IOriginalActionHandler = IOriginalActionHandler<INavigateFromHereProvider, ContextNavigation, NavigationActionGroup>;

    public class NavigateFromHereAction : ContextNavigationActionBase<INavigateFromHereProvider>,
        IExecutableAction, IOriginalActionHandler
    {
        private readonly ExtensibleActionHelper actionHelper;

        public NavigateFromHereAction(Lifetime lifetime, Agent agent, IActionManager actionManager)
        {
            actionHelper = new ExtensibleActionHelper(lifetime, this, agent, actionManager);
        }

        void IExecutableAction.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            actionHelper.Execute(dataContext, nextExecute);
        }

        protected override bool ShowMenuWithOneItem => true;

        protected override RichText Caption => "Navigate to";


        ICollection<INavigateFromHereProvider> IOriginalActionHandler.GetWorkflowProviders()
        {
            return GetWorkflowProviders();
        }

        int IOriginalActionHandler.CompareWorkflowItems(ValueTuple<ContextNavigation, INavigateFromHereProvider> item1,
            ValueTuple<ContextNavigation, INavigateFromHereProvider> item2)
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

        bool IOriginalActionHandler.ShowMenuWithOneItem => ShowMenuWithOneItem;

        RichText IOriginalActionHandler.Caption => Caption;
    }
}