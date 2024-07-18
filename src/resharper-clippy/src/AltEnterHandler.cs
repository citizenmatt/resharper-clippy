using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Controls.BulbMenu;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Keys;
using JetBrains.Diagnostics;
using JetBrains.IDE.PerClientComponents;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Intentions.Bulbs;
using JetBrains.ReSharper.Resources.Shell;

namespace CitizenMatt.ReSharper.Plugins.Clippy
{
    [ShellComponent]
    public class AltEnterHandler(Lifetime lifetime, Agent agent) : IAltEnterHandler
    {
        public bool IsAvailable(IDataContext context) => true;

        public bool HandleAction(IDataContext context)
        {
            var bulbActionKeys = GetBulbActionKeys(context);
            if (bulbActionKeys == null)
                return false;

            var options = new List<BalloonOption>();
            PopulateBalloonOptions(options, bulbActionKeys);

            var buttons = new List<string> {"Cancel"};

            var lifetimeDefinition = lifetime.CreateNested();

            agent.ShowBalloon(lifetimeDefinition.Lifetime, "What do you want to do?", string.Empty,
                options, buttons, true, Init);

            return true;

            void Init(Lifetime balloonLifetime)
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
            }
        }

        public double Priority => 0;

        private IEnumerable<BulbActionKey> GetBulbActionKeys(IDataContext context)
        {
            var solution = context.GetData(ProjectModelDataConstants.SOLUTION).NotNull();
            if (solution.GetCurrentClientSession().GetComponent<BulbItems>().BulbItemsState.Value is BulbItemsReadyState readyState)
            {
                var keys = BulbKeysBuilder.BuildMenuKeys(readyState.IntentionsBulbItems.CollectAllBulbMenuItems());
                return keys.Count > 0 ? keys : null;
            }

            return null;
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