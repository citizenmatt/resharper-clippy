using System;
using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Intentions.Bulbs;
using JetBrains.UI.BulbMenu;
using DataConstants = JetBrains.TextControl.DataContext.DataConstants;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [ShellComponent]
    public class AltEnterHandler : IAltEnterHandler
    {
        private readonly Lifetime lifetime;
        private readonly Agent agent;
        private readonly BulbKeysBuilder bulbKeysBuilder;
        private bool setPosition = true;

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

        // TODO: Take keyboard focus. Handle escape + keyboard
        // TODO: Sub menus. Indicator for submenus?
        public bool HandleAction(IDataContext context)
        {
            // TODO: Positioning should be in a common place
            var textControl = context.GetData(DataConstants.TEXT_CONTROL);
            if (textControl != null && setPosition)
            {
                Lifetimes.Using(l =>
                {
                    var rect = textControl.Window.CreateViewportAnchor(l);
                    agent.SetLocation(rect.Rectangle.Value.Right - 250, rect.Rectangle.Value.Bottom - 250);
                });

                setPosition = false;
            }

            var bulbItems = context.GetComponent<BulbItems>();
            if (bulbItems.BulbItemsState.Value == null)
                return false;

            var bulbItemsState = bulbItems.BulbItemsState.Value;
            if (bulbItemsState.BulbItemsStates == BulbItemsStates.Invalidated)
                return false;

            if (bulbItemsState.IntentionsBulbItems == null || !bulbItemsState.IntentionsBulbItems.AllBulbMenuItems.Any())
                return false;

            var bulbActionKeys = bulbKeysBuilder.BuildMenuKeys(bulbItemsState.IntentionsBulbItems.AllBulbMenuItems);

            // TODO: Keyboard shortcut indicators?
            var options = new List<BalloonOption>();
            IAnchor groupingAnchor = null;
            foreach (var key in bulbActionKeys)
            {
                if (groupingAnchor == null)
                    groupingAnchor = key.GroupingAnchor;

                if (key.RichText == null)
                    continue;

                var requiresSeparator = !Equals(key.GroupingAnchor, groupingAnchor);
                groupingAnchor = key.GroupingAnchor;
                options.Add(new BalloonOption(key.RichText.ToString(), requiresSeparator, key));
            }

            var buttons = new List<string> {"Cancel"};

            var lifetimeDefinition = Lifetimes.Define(lifetime);
            Action<Lifetime> init = balloonLifetime =>
            {
                agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                {
                    // ReSharper disable once ConvertToLambdaExpression
                    ReadLockCookie.GuardedExecute(() =>
                    {
                        lifetimeDefinition.Terminate();
                        var action = (BulbActionKey)o;
                        action.Clicked();
                    });
                });

                agent.ButtonClicked.Advise(balloonLifetime, _ => lifetimeDefinition.Terminate());
            };

            agent.ShowBalloon(lifetimeDefinition.Lifetime, "What do you want to do?",
                "(Note: no support for submenus or keyboard yet)",
                options, buttons, init);

            return true;
        }

        public double Priority { get { return 0; } }
    }
}