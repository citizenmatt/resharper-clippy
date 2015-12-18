using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.ActionsMenu;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.ActionSystem.ActionManager;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    // This is messy, but necessary. We want to replace the menu that gets created
    // in ExtensibleAction, but there are no easy overrides, so we have to do most
    // of the leg work ourselves. To make matters more interesting, we can't be a
    // base class, because the actions we want to override have another class in
    // between themselves and ExtensibleAction. So, we defer to the helper class,
    // which defers back to the original base class via an interface which exposes
    // protected methods. Ugh. I need a shower now.
    public class AgentExtensibleAction<TWorkflowProvider, TWorkflow, TActionGroup>
        where TWorkflowProvider : class, IWorkflowProvider<TWorkflow, TActionGroup>
        where TWorkflow : class, IWorkflow<TActionGroup>
        where TActionGroup : ExtensibleActionGroup
    {
        private readonly Lifetime lifetime;
        private readonly IOriginalActionHandler<TWorkflowProvider, TWorkflow, TActionGroup> handler;
        private readonly Agent agent;
        private readonly IActionManager actionManager;

        public AgentExtensibleAction(Lifetime lifetime,
            IOriginalActionHandler<TWorkflowProvider, TWorkflow, TActionGroup> handler,
            Agent agent, IActionManager actionManager)
        {
            this.lifetime = lifetime;
            this.handler = handler;
            this.agent = agent;
            this.actionManager = actionManager;
        }

        public void Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            using (ReadLockCookie.Create())
            {
                var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
                if (solution == null)
                {
                    nextExecute();
                    return;
                }

                using (CompilationContextCookie.Create(GetResolveContext(dataContext, solution)))
                {
                    // The real code creates a modal loop here, so can use the data
                    // We use a modeless loop, so make sure the data context lasts
                    // for longer
                    var dataContextLifetimeDefinition = Lifetimes.Define(lifetime);
                    var extendedContext = dataContext.Prolongate(dataContextLifetimeDefinition.Lifetime);

                    var toExecute = GetWorkflowListToExecute(extendedContext);
                    if (toExecute == null || toExecute.Count == 0)
                    {
                        dataContextLifetimeDefinition.Terminate();
                        nextExecute();
                        return;
                    }

                    if (toExecute.HasMoreThan(1) || handler.ShowMenuWithOneItem)
                    {
                        ExecuteGroup(extendedContext, toExecute, dataContextLifetimeDefinition);
                        return;
                    }

                    handler.Execute(extendedContext, toExecute.Single().First);
                    dataContextLifetimeDefinition.Terminate();
                };
            }
        }

        private IModuleReferenceResolveContext GetResolveContext(IDataContext context, ISolution solution)
        {
            var data = context.GetData(DocumentModelDataContants.DOCUMENT);
            if (data == null)
                return UniversalModuleReferenceContext.Instance;
            var psiSourceFile = data.GetPsiSourceFile(solution);
            return psiSourceFile == null ? UniversalModuleReferenceContext.Instance : psiSourceFile.ResolveContext;
        }


        private List<Pair<TWorkflow, TWorkflowProvider>> GetWorkflowListToExecute(IDataContext dataContext)
        {
            var providers = handler.GetWorkflowProviders();
            if (providers.Count == 0)
            {
                Logger.Fail("Provider of type '{0}' has no implementations.", typeof(TWorkflowProvider));
                return null;
            }

            var toExecute = new List<Pair<TWorkflow, TWorkflowProvider>>();

            // check is there are available overridden providers...
            var overriddenProviders = new LocalList<TWorkflowProvider>();
            foreach (var provider in providers)
            {
                var overridingProvider = provider as IOverridingWorkflowProvider;
                if (overridingProvider == null ||
                    !overridingProvider.HideOtherActions(dataContext)) continue;

                overriddenProviders.Add((TWorkflowProvider)overridingProvider);
            }

            if (overriddenProviders.Count > 0)
                providers = overriddenProviders.ResultingList();

            foreach (var workflowProvider in providers)
            {
                var workflows = workflowProvider.CreateWorkflow(dataContext);
                foreach (var workflow in workflows)
                {
                    if (workflow != null && handler.IsAvailable(dataContext, workflow))
                    {
                        toExecute.Add(Pair.Of(workflow, workflowProvider));
                    }
                }
            }

            return toExecute;
        }

        private void ExecuteGroup(IDataContext context,
            IEnumerable<Pair<TWorkflow, TWorkflowProvider>> workflows,
            LifetimeDefinition dataContextLifetimeDefinition)
        {
            var groups = workflows.GroupBy(x => x.First.ActionGroup).ToList();
            groups.Sort((g1, g2) =>
            {
                var delta = g1.Key.Order - g2.Key.Order;
                if (delta == 0) return 0;
                return delta > 0 ? 1 : -1;
            });
            
            var options = new List<BalloonOption>();

            var showSeparatorForFirstItem = false;
            foreach (var group in groups)
            {
                var items = group.ToList();
                items.Sort(handler.CompareWorkflowItems);

                var isFirst = showSeparatorForFirstItem;
                foreach (var item in items)
                {
                    if (handler.IsEnabled(context, item.First))
                    {
                        var text = item.First.Title + GetShortcut(item.First);
                        options.Add(new BalloonOption(text, isFirst, true, item.First));

                        isFirst = false;
                    }
                }
                showSeparatorForFirstItem = true;
            }

            var balloonLifetimeDefinition = Lifetimes.Define(lifetime);

            Action<Lifetime> init = balloonLifetime =>
            {
                agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                {
                    balloonLifetimeDefinition.Terminate();

                    var workflow = o as TWorkflow;
                    if (workflow == null)
                        return;

                    ReadLockCookie.GuardedExecute(() =>
                    {
                        handler.Execute(context, workflow);
                        dataContextLifetimeDefinition.Terminate();
                    });
                });

                agent.ButtonClicked.Advise(balloonLifetime, () =>
                {
                    balloonLifetimeDefinition.Terminate();
                    dataContextLifetimeDefinition.Terminate();
                });
            };

            agent.ShowBalloon(balloonLifetimeDefinition.Lifetime, handler.Caption, string.Empty,
                options, new[] {"Cancel"}, true, init);
        }

        private string GetShortcut(IWorkflow<TActionGroup> workflow)
        {
            foreach (var actionId in new[] {workflow.ActionId, workflow.ShortActionId})
            {
                if (string.IsNullOrEmpty(actionId))
                    continue;

                var action = actionManager.Defs.TryGetActionDefById(actionId);
                if (action == null)
                    continue;

                var shortcutText = actionManager.Shortcuts.GetShortcutString(action);
                if (!string.IsNullOrEmpty(shortcutText))
                    return string.Format(" ({0})", shortcutText);
            }
            return string.Empty;
        }
    }
}