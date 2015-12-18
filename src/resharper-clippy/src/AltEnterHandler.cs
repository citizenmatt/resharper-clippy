using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Intentions.Bulbs;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.BulbMenu;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [ShellComponent]
    public class AltEnterHandler : IAltEnterHandler
    {
        private readonly Lifetime lifetime;
        private readonly Agent agent;
        private readonly BulbKeysBuilder bulbKeysBuilder;

        public AltEnterHandler(Lifetime lifetime, Agent agent)
        {
            this.lifetime = lifetime;
            this.agent = agent;
            bulbKeysBuilder = new BulbKeysBuilder();
        }

        public bool IsAvailable(IDataContext context)
        {
            return true;
        }

        public bool HandleAction(IDataContext context)
        {
            var bulbActionKeys = GetBulbActionKeys(context);
            if (bulbActionKeys == null)
                return false;

            var options = new List<BalloonOption>();
            PopulateBalloonOptions(options, bulbActionKeys);

            var buttons = new List<string> {"Cancel"};

            var lifetimeDefinition = Lifetimes.Define(lifetime);
            Action<Lifetime> init = balloonLifetime =>
            {
                agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                {
                    ReadLockCookie.GuardedExecute(() =>
                    {
                        lifetimeDefinition.Terminate();
                        var action = (BulbActionKey)o;
                        action.Clicked();
                    });
                });

                agent.ButtonClicked.Advise(balloonLifetime, _ => lifetimeDefinition.Terminate());
            };

            agent.ShowBalloon(lifetimeDefinition.Lifetime, "What do you want to do?", string.Empty,
                options, buttons, true, init);

            return true;
        }

        public double Priority { get { return 0; } }

        private IEnumerable<BulbActionKey> GetBulbActionKeys(IDataContext context)
        {
            var bulbItems = context.GetComponent<BulbItems>();
            if (bulbItems.BulbItemsState.Value == null)
                return null;

            var bulbItemsState = bulbItems.BulbItemsState.Value;
            if (bulbItemsState.BulbItemsStates == BulbItemsStates.Invalidated)
                return null;

            if (bulbItemsState.IntentionsBulbItems == null || !bulbItemsState.IntentionsBulbItems.AllBulbMenuItems.Any())
                return null;

            var bulbActionKeys = bulbKeysBuilder.BuildMenuKeys(bulbItemsState.IntentionsBulbItems.AllBulbMenuItems);
            return bulbActionKeys;
        }

        private void PopulateBalloonOptions(IList<BalloonOption> options, IEnumerable<BulbActionKey> bulbActions)
        {
            IAnchor groupingAnchor = null;
            foreach (var bulbAction in bulbActions)
            {
                if (groupingAnchor == null)
                    groupingAnchor = bulbAction.GroupingAnchor;

                if (bulbAction.RichText == null)
                    continue;

                var enabled = bulbAction.Executable != null;

                var requiresSeparator = !Equals(bulbAction.GroupingAnchor, groupingAnchor);
                groupingAnchor = bulbAction.GroupingAnchor;
                options.Add(new BalloonOption(bulbAction.RichText.Text, requiresSeparator, enabled, bulbAction));

                PopulateBalloonOptions(options, bulbAction.Subitems);
            }
        }
    }
}