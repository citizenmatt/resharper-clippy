using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Intentions.Bulbs;
using JetBrains.Threading;
using JetBrains.UI.BulbMenu;
using DataConstants = JetBrains.TextControl.DataContext.DataConstants;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [ShellComponent]
    public class AltEnterHandler : IAltEnterHandler
    {
        private readonly Agent agent;
        private readonly IShellLocks locks;
        private readonly IThreading threading;
        private readonly BulbKeysBuilder bulbKeysBuilder;
        private bool setPosition = true;

        public AltEnterHandler(Agent agent, IShellLocks locks, IThreading threading)
        {
            this.agent = agent;
            this.locks = locks;
            this.threading = threading;
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
            {
                return false;
            }

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

// ReSharper disable ConvertToLambdaExpression
            agent.Ask("What do you want to do?", "(Note: no support for submenus or keyboard yet)", new List<string> { "Cancel" }, options,
                balloonLifetime =>
                {
                    agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                    {
                        ReadLockCookie.GuardedExecute(() =>
                        {
                            locks.AssertReadAccessAllowed();
                            var action = o as BulbActionKey;
                            if (action == null)
                                return;
                            agent.HideBalloon();
                            action.Clicked();
                        });
                    });

                    agent.ButtonClicked.Advise(balloonLifetime, _ => agent.HideBalloon());
                });
// ReSharper restore ConvertToLambdaExpression

            return true;
        }

        public double Priority { get { return 0; } }
    }
}