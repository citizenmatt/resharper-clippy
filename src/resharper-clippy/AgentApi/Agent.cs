using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DataFlow;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    [ShellComponent]
    public class Agent
    {
        private readonly JetBrains.Util.Lazy.Lazy<AgentCharacter> character;

        public Agent(Lifetime lifetime, AgentManager agentManager)
        {
            character = JetBrains.Util.Lazy.Lazy.Of(() =>
            {
                agentManager.Initialise();
                var agentCharacter = agentManager.GetAgent("Clippit");

                agentCharacter.AgentClicked.FlowInto(lifetime, AgentClicked);
                agentCharacter.ButtonClicked.FlowInto(lifetime, ButtonClicked);
                agentCharacter.BalloonOptionClicked.FlowInto(lifetime, BalloonOptionClicked);

                lifetime.AddAction(() => agentManager.UnloadAgent(agentCharacter));

                return agentCharacter;
            });

            AgentClicked = new SimpleSignal(lifetime, "Agent::AgentClicked");
            ButtonClicked = new Signal<string>(lifetime, "Agent::ButtonClicked");
            BalloonOptionClicked = new Signal<object>(lifetime, "Agent::BalloonOptionClicked");
        }

        private void Do(Action<AgentCharacter> action)
        {
            var c = character.Value;
            if (c != null)
                action(c);
        }

        private T Do<T>(Func<AgentCharacter, T> action)
        {
            var c = character.Value;
            return c != null ? action(c) : default(T);
        }

        public bool IsVisible
        {
            get { return Do(c => c.Visible); }
        }

        public void SetLocation(double x, double y)
        {
            Do(c => c.MoveTo((short) x, (short) y));
        }

        public void Show()
        {
            Do(c => c.Show());
        }

        public void Hide()
        {
            // TODO: Look at request? Fire signal when it's finished?
            Do(c => c.Hide());
        }

        public void ShowBalloon(Lifetime lifetime, string header, string message, IList<BalloonOption> options,
            IEnumerable<string> buttons, bool activate, Action<Lifetime> init)
        {
            Do(c => c.ShowBalloon(lifetime, header, message, options, buttons, activate, init));
        }

        public ISimpleSignal AgentClicked { get; private set; }
        public ISignal<string> ButtonClicked { get; private set; }
        public IUntypedSignal BalloonOptionClicked { get; private set; }
    }
}