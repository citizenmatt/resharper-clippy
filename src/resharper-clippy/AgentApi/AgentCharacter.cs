using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DoubleAgent.Control;
using JetBrains.DataFlow;
using JetBrains.Util.Interop;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class AgentCharacter : ICharacterEvents
    {
        private readonly AgentManager agentManager;
        private readonly BalloonManager balloon;
        private readonly IWin32Window characterWindow;
        private readonly IDictionary<int, Action> requestHandlers; 

        public AgentCharacter(Lifetime lifetime, Character character, AgentManager agentManager)
        {
            this.agentManager = agentManager;
            Character = character;
            ScaleCharacterForDpi();

            AgentClicked = new SimpleSignal(lifetime, "AgentCharacter::AgentClicked");
            ButtonClicked = new Signal<string>(lifetime, "AgentCharacter::ButtonClicked");
            BalloonOptionClicked = new Signal<object>(lifetime, "AgentCharacter::BalloonOptionClicked");

            balloon = new BalloonManager(lifetime);
            balloon.ButtonClicked.FlowInto(lifetime, ButtonClicked);
            balloon.BalloonOptionClicked.FlowInto(lifetime, BalloonOptionClicked);

            requestHandlers = new Dictionary<int, Action>();

            characterWindow = OleWin32Window.FromIOleWindow(character.Interface);
        }

        public Character Character { get; private set; }

        private void RegisterRequest(Request request)
        {
            agentManager.RegisterRequest(request, this);
        }

        private void ScaleCharacterForDpi()
        {
            Character.SetSize((short)(Character.OriginalWidth * DpiUtil.DpiHorizontalFactor),
                (short)(Character.OriginalHeight * DpiUtil.DpiVerticalFactor));
        }

        public void Hide(bool fancy = false)
        {
            if (fancy && Visible)
            {
                balloon.ForceHide();
                Play("Goodbye", () =>
                {
                    if (Visible)
                        RegisterRequest(Character.Hide(true));
                });
                return;
            }

            RegisterRequest(Character.Hide());
            balloon.ForceHide();
        }

        public void MoveTo(short x, short y)
        {
            RegisterRequest(Character.MoveTo(x, y));
            balloon.UpdateAnchorPoint(x, y, Character.Width, Character.Height);
        }

        public void Show(bool fancy = false)
        {
            if (fancy && !Visible)
            {
                Character.Show(true);
                Play("Greeting");
                return;
            }
            RegisterRequest(Character.Show());
        }

        public void Play(string animation)
        {
            var request = Character.Play(animation);
            RegisterRequest(request);
        }

        public void Play(string animation, Action onComplete)
        {
            var request = Character.Play(animation);
            requestHandlers.Add(request.ID, onComplete);
            RegisterRequest(request);
        }

        public void Play(Lifetime lifetime, string animation)
        {
            var request = Character.Play(animation);
            RegisterRequest(request);
            lifetime.AddAction(() =>
            {
                if (request.Status == (int)RequestStatus.InProgress)
                    Character.Play("Idle1_1");
            });
        }

        public void ShowBalloon(Lifetime clientLifetime, string header, string message,
            IList<BalloonOption> options, IEnumerable<string> buttons, bool activate, Action<Lifetime> init)
        {
            if (!Character.Visible)
                Show();

            balloon.CreateNew(clientLifetime, balloonLifetime =>
            {
                balloon.SetText(header, message);
                balloon.SetOptions(options);
                balloon.SetButtons(buttons);

                init(balloonLifetime);

                balloon.Show(characterWindow, Character.Left, Character.Top, Character.Width, Character.Height, activate);
            });
        }

        public bool Visible
        {
            get { return Character.Visible; }
        }

        public ISimpleSignal AgentClicked { get; private set; }

        /// <summary>
        /// Passes through the string of the button text
        /// </summary>
        public ISignal<string> ButtonClicked { get; private set; }

        /// <summary>
        /// Passes through the object of the option's Tag
        /// </summary>
        public IUntypedSignal BalloonOptionClicked { get; private set; }

        void ICharacterEvents.OnRequestStart(Request request)
        {
        }

        void ICharacterEvents.OnRequestComplete(Request request)
        {
            Action handler;
            if (requestHandlers.TryGetValue(request.ID, out handler))
            {
                handler();
                requestHandlers.Remove(request.ID);
            }
        }

        void ICharacterEvents.OnMove(short x, short y, MoveCauseType cause)
        {
            balloon.UpdateAnchorPoint(x, y, Character.Width, Character.Height);
        }

        void ICharacterEvents.OnClick(short button, bool shiftKey, short x, short y)
        {
            // 1 for left, 2 for right, 4 for middle. Presumably flags?
            if (button == 1)
                AgentClicked.Fire();
        }

        void ICharacterEvents.OnCommand(UserInput userInput)
        {
        }

        void ICharacterEvents.OnDoubleClick(short button, bool shiftKey, short x, short y)
        {
        }

        void ICharacterEvents.OnDragStart(short button, bool shiftKey, short x, short y)
        {
        }

        void ICharacterEvents.OnDragComplete(short button, bool shiftKey, short x, short y)
        {
        }

        void ICharacterEvents.OnHide(VisibilityCauseType cause)
        {
            balloon.ForceHide();
        }

        void ICharacterEvents.OnShow(VisibilityCauseType cause)
        {
        }
    }
}