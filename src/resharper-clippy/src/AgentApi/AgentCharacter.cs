using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DoubleAgent.Control;
using JetBrains.DataFlow;
using JetBrains.Extension;
using JetBrains.Util.Interop;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class AgentCharacter : ICharacterEvents
    {
        private readonly AgentManager agentManager;
        private readonly IWin32Window owner;
        private readonly BalloonManager balloon;
        private readonly IWin32Window characterWindow;
        private readonly IDictionary<int, Action> requestHandlers;
        private readonly Random random;
        private Action initLocation;

        public AgentCharacter(Lifetime lifetime, Character character, AgentManager agentManager, IWin32Window owner)
        {
            this.agentManager = agentManager;
            this.owner = owner;
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
            characterWindow.SetOwner(owner);

            initLocation = SetDefaultLocation;

            random = new Random();
        }

        public Character Character { get; private set; }

        // Requests are a PITA. Everything you do returns a request so you can track it
        // and act on completion. They're queued, so happen consecutively. But certain
        // animations are looping, and you don't know which ones, so you have to call
        // Stop before queueing up any more animations. But that doesn't clear the queue
        // so another looping animation might happen before you get in, so you have to
        // clear all animations, but not move requests, because they might be useful.
        // Oh, and the request start/end notifications are sent to the agent control,
        // not the character, so we have to register the request and the character with
        // the control, so we route effectively. I'd love to see how Office got this
        // horrible API to work so well for them
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
            // Stop everything and flush the queue before hiding
            StopAll();

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
            // Queue the move up, after the current animation
            // TODO: Is that wise? Is the agent smart enough to do both?
            RegisterRequest(Character.MoveTo(x, y));
            balloon.UpdateAnchorPoint(x, y, Character.Width, Character.Height);
        }

        public void Show(bool fancy = false)
        {
            initLocation();

            if (fancy && !Visible)
            {
                RegisterRequest(Character.Show(true));
                Play("Greeting");
                return;
            }
            RegisterRequest(Character.Show());
        }

        private void SetDefaultLocation()
        {
            var ownerBounds = owner.GetBounds();

            // If the owner is more than 3 times as high as the character, show it in the window corner, else show it in the screen corner
            if (Character.Height < (ownerBounds.Height/3)
                && Character.Width < (ownerBounds.Width/4))
            {
                MoveTo((short)(ownerBounds.Right - (Character.Width * 1.5)),
                    (short)(ownerBounds.Bottom - (Character.Height * 1.5)));
            }
            else
            {
                var screen = Screen.FromHandle(owner.Handle);

                MoveTo((short)(screen.WorkingArea.Right - (Character.Width * 1.5)),
                    (short)(screen.WorkingArea.Bottom - (Character.Height * 1.5)));
            }

            initLocation = () => { };
        }

        public void PlayRandom()
        {
            // Stop the current, potentially looping animation, and any other
            // (potentially looping) animations in the queue before playing ours
            StopAllAnimations();

            var names = Character.Animations;
            var name = names[random.Next(names.Length)];
            Play(name);
        }

        public void Play(string animation)
        {
            // Stop the current, potentially looping animation, and any other
            // (potentially looping) animations in the queue before playing ours
            StopAllAnimations();

            RegisterRequest(Character.Play(animation));
        }

        public void Play(string animation, Action onComplete)
        {
            // Stop the current, potentially looping animation, and any other
            // (potentially looping) animations in the queue before playing ours
            StopAllAnimations();

            var request = Character.Play(animation);
            requestHandlers.Add(request.ID, onComplete);
            RegisterRequest(request);
        }

        public void Play(Lifetime lifetime, string animation)
        {
            // Stop the current, potentially looping animation, and any other
            // (potentially looping) animations in the queue before playing ours
            StopAllAnimations();

            var request = Character.Play(animation);
            RegisterRequest(request);
            lifetime.AddAction(() =>
            {
                var requestStatus = (RequestStatus) request.Status;
                if (requestStatus == RequestStatus.Pending || requestStatus == RequestStatus.InProgress)
                    Stop(request);
            });
        }

        public void StopAllAnimations()
        {
            // Stop the current and all queued animations, but leave any other requests alone
            Character.StopAll("Play");
        }

        public void StopAll()
        {
            // Stop everything and flush the queue
            Character.StopAll();
        }

        public void Stop(Request request)
        {
            // Just stop this request
            Character.Stop(request.Interface);
        }

        public void ShowBalloon(Lifetime clientLifetime, string header, string message,
            IList<BalloonOption> options, IEnumerable<string> buttons, bool activate, Action<Lifetime> init)
        {
            // Stop all animations, should stop the idle animation when we're doing something
            StopAllAnimations();

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
            if (button == 2)
            {
                var menuStrip = new ContextMenuStrip();
                menuStrip.Items.Add("Hide", null, (_, __) => Hide());
                menuStrip.Items.Add("Animate", null, (_, __) => PlayRandom());
                menuStrip.Show(x, y);
            }
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