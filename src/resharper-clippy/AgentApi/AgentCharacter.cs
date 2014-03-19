using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DoubleAgent.Control;
using JetBrains.DataFlow;
using JetBrains.Interop.WinApi;
using JetBrains.Interop.WinApi.Wrappers;
using JetBrains.Util.Interop;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class AgentCharacter : ICharacterEvents
    {
        private readonly BalloonManager balloon;

        public AgentCharacter(Lifetime lifetime, Character character)
        {
            Character = character;
            ScaleCharacterForDpi();

            AgentClicked = new SimpleSignal(lifetime, "AgentCharacter::AgentClicked");
            ButtonClicked = new Signal<string>(lifetime, "AgentCharacter::ButtonClicked");
            BalloonOptionClicked = new Signal<object>(lifetime, "AgentCharacter::BalloonOptionClicked");

            balloon = new BalloonManager(lifetime);
            balloon.ButtonClicked.FlowInto(lifetime, ButtonClicked);
            balloon.BalloonOptionClicked.FlowInto(lifetime, BalloonOptionClicked);
        }

        public Character Character { get; private set; }

        private void ScaleCharacterForDpi()
        {
            Character.SetSize((short)(Character.OriginalWidth * DpiUtil.DpiHorizontalFactor),
                (short)(Character.OriginalHeight * DpiUtil.DpiVerticalFactor));
        }

        public void Hide()
        {
            Character.Hide();
            balloon.ForceHide();
        }

        public void MoveTo(short x, short y)
        {
            Character.MoveTo(x, y);
            balloon.UpdateAnchorPoint(x, y, Character.Width, Character.Height);
        }

        public void Show()
        {
            Character.Show();
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

                // TODO: Yuck. Is this the best idea?
                var owner = new Win32Window(WindowFromPoint(new POINT(Character.Left, Character.Top)));
                balloon.Show(owner, Character.Left, Character.Top, Character.Width, Character.Height, activate);
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

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr WindowFromPoint(POINT point);
    }
}