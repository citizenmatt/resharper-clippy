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

        public AgentCharacter AgentCharacter
        {
            get { return character.Value; }
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

        public void Show(bool fancy = false)
        {
            Do(c => c.Show(fancy));
        }

        public void Hide(bool fancy = false)
        {
            Do(c => c.Hide(fancy));
        }

        public void Play(string animation)
        {
            Do(c => c.Play(animation));
        }

        public void Play(Lifetime lifetime, string animation)
        {
            Do(c => c.Play(lifetime, animation));
        }

        public void StopAllAnimations()
        {
            Do(c => c.StopAllAnimations());
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