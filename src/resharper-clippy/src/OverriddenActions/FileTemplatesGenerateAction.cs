using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.ActionsMenu;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.LiveTemplates.FileTemplates;
using JetBrains.UI.ActionSystem.ActionManager;
using JetBrains.Util;
using JetBrains.Util.Logging;
using DataConstants = JetBrains.ProjectModel.DataContext.DataConstants;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public class FileTemplatesGenerateAction : GenerateActionBase<GenerateFromTemplateItemProvider>, IActionHandler
    {
        private readonly Lifetime lifetime;
        private readonly Agent agent;
        private readonly IActionManager actionManager;
        private readonly IShortcutManager shortcutManager;

        public FileTemplatesGenerateAction(Lifetime lifetime, Agent agent, IActionManager actionManager, IShortcutManager shortcutManager)
        {
            this.lifetime = lifetime;
            this.agent = agent;
            this.actionManager = actionManager;
            this.shortcutManager = shortcutManager;
        }

        void IActionHandler.Execute(IDataContext dataContext, DelegateExecute nextExecute)
        {
            using (ReadLockCookie.Create())
            {
                var solution = dataContext.GetData(DataConstants.SOLUTION);
                if (solution == null)
                {
                    nextExecute();
                    return;
                }

                var asyncActionWait = new AsyncActionWaiter(dataContext);

                asyncActionWait.Execute(newDataContext =>
                {
                    // The real code creates a modal loop here, so can use the data
                    // We use a modeless loop, so make sure the data context lasts
                    // for longer
                    var dataContextLifetimeDefinition = Lifetimes.Define(lifetime);
                    var extendedContext = newDataContext.Prolongate(dataContextLifetimeDefinition.Lifetime);

                    var toExecute = GetWorkflowListToExecute(extendedContext);
                    if (toExecute == null || toExecute.Count == 0)
                    {
                        dataContextLifetimeDefinition.Terminate();
                        nextExecute();
                        return;
                    }

                    if (toExecute.HasMoreThan(1) || ShowMenuWithOneItem)
                    {
                        ExecuteGroup(extendedContext, toExecute, dataContextLifetimeDefinition);
                        return;
                    }

                    Execute(extendedContext, toExecute.Single().First);
                    dataContextLifetimeDefinition.Terminate();
                });
            }
        }

        [CanBeNull]
        private List<Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider>> GetWorkflowListToExecute(IDataContext dataContext)
        {
            var providers = GetWorkflowProviders();
            if (providers.Count == 0)
            {
                Logger.Fail("Provider of type '{0}' has no implementations.", typeof(GenerateFromTemplateItemProvider));
                return null;
            }

            var toExecute = new List<Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider>>();

            // check is there are available overridden providers...
            var overriddenProviders = new LocalList<GenerateFromTemplateItemProvider>();
            foreach (var provider in providers)
            {
                var overridingProvider = provider as IOverridingWorkflowProvider;
                if (overridingProvider == null ||
                   !overridingProvider.HideOtherActions(dataContext)) continue;

                overriddenProviders.Add((GenerateFromTemplateItemProvider)overridingProvider);
            }

            if (overriddenProviders.Count > 0)
                providers = overriddenProviders.ResultingList();

            foreach (var workflowProvider in providers)
            {
                var workflows = workflowProvider.CreateWorkflow(dataContext);
                foreach (var workflow in workflows)
                {
                    if (workflow != null && IsAvailable(dataContext, workflow))
                    {
                        toExecute.Add(Pair.Of(workflow, workflowProvider));
                    }
                }
            }

            return toExecute;
        }

        private void ExecuteGroup(IDataContext context,
            IEnumerable<Pair<IGenerateActionWorkflow, GenerateFromTemplateItemProvider>> workflows,
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

            foreach (var group in groups)
            {
                var items = group.ToList();
                items.Sort(CompareWorkflowItems);

                var isFirst = true;
                foreach (var item in items)
                {
                    if (IsEnabled(context, item.First))
                    {
                        var text = item.First.Title + GetShortcut(item.First);
                        options.Add(new BalloonOption(text, isFirst, true, item.First));

                        isFirst = false;
                    }
                }
            }

            var balloonLifetimeDefinition = Lifetimes.Define(lifetime);

            Action<Lifetime> init = balloonLifetime =>
            {
                agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                {
                    balloonLifetimeDefinition.Terminate();

                    var workflow = o as IGenerateActionWorkflow;
                    if (workflow == null)
                        return;

                    ReadLockCookie.GuardedExecute(() =>
                    {
                        Execute(context, workflow);
                        dataContextLifetimeDefinition.Terminate();
                    });
                });

                agent.ButtonClicked.Advise(balloonLifetime, () =>
                {
                    balloonLifetimeDefinition.Terminate();
                    dataContextLifetimeDefinition.Terminate();
                });
            };

            agent.ShowBalloon(balloonLifetimeDefinition.Lifetime, "Create File From Template", string.Empty,
                options, new[] {"Cancel"}, true, init);
        }

        private string GetShortcut(IWorkflow<GenerateActionGroup> workflow)
        {
            foreach (var actionId in new[] {workflow.ActionId, workflow.ShortActionId})
            {
                if (string.IsNullOrEmpty(actionId))
                    continue;

                var action = actionManager.TryGetAction(actionId) as IExecutableAction;
                if (action == null)
                    continue;

                var shortcutText = shortcutManager.GetShortcutString(action);
                if (!string.IsNullOrEmpty(shortcutText))
                    return string.Format(" ({0})", shortcutText);
            }
            return string.Empty;
        }
    }
}