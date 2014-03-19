using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon;
using JetBrains.DataFlow;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class BalloonManager
    {
        private readonly SequentialLifetimes balloonLifetimes;
        private BalloonWindow balloonWindow;
        private BalloonWindowHost host;

        public BalloonManager(Lifetime overallLifetime)
        {
            balloonLifetimes = new SequentialLifetimes(overallLifetime);
            ButtonClicked = new Signal<string>(overallLifetime, "BalloonManager::ButtonClicked");
            BalloonOptionClicked = new Signal<object>(overallLifetime, "BalloonManager::BalloonOptionClicked");
        }

        public void CreateNew(Lifetime clientLifetime, Action<Lifetime> init)
        {
            balloonLifetimes.Next(balloonLifetime =>
            {
                balloonWindow = new BalloonWindow();
                balloonWindow.ButtonClicked += OnButtonClicked;
                balloonWindow.OptionClicked += OnBalloonOptionClicked;

                host = new BalloonWindowHost(balloonWindow);

                // If the client wants to hide the balloon, they can terminate clientLifetime
                // If another client calls CreateNew, balloonLifetimes.Next terminates
                // balloonLifetime. Whichever lifetime terminates first will cause
                // combinedLifetime to terminate, closing the window. 
                var combinedLifetime = Lifetimes.CreateIntersection2(clientLifetime, balloonLifetime).Lifetime;
                combinedLifetime.AddAction(() =>
                {
                    if (host != null)
                    {
                        host.Close();
                        host = null;
                        balloonWindow = null;
                    }
                });

                init(combinedLifetime);
            });
        }

        /// <summary>
        /// You most likely don't want to call this
        /// </summary>
        /// <remarks>
        /// This will hide whatever balloon is currently being shown, even if it's not yours
        /// </remarks>
        public void ForceHide()
        {
            balloonLifetimes.TerminateCurrent();
        }

        public void Show(IWin32Window owner, short left, short top, short width, short height, bool activate)
        {
            UpdateAnchorPoint(left, top, width, height);
            host.Show(owner, activate);
        }

        public void UpdateAnchorPoint(short left, short top, short width, short height)
        {
            if (host != null)
                host.SetAnchorBounds(left, top, width, height);
        }

        public void SetText(string header, string message)
        {
            if (header != null)
                balloonWindow.Header = header;
            balloonWindow.Message = message;
        }

        public void SetButtons(IEnumerable<string> buttons)
        {
            balloonWindow.SetButtons(buttons);
        }

        public void SetOptions(IList<BalloonOption> options)
        {
            balloonWindow.SetOptions(options);
        }

        private void OnButtonClicked(object sender, BalloonActionEventArgs<string> args)
        {
            ButtonClicked.Fire(args.Tag);
        }

        private void OnBalloonOptionClicked(object sender, BalloonActionEventArgs<object> args)
        {
            BalloonOptionClicked.Fire(args.Tag, null);
        }

        /// <summary>
        /// Passes through the string of the button
        /// </summary>
        public ISignal<string> ButtonClicked { get; private set; }

        /// <summary>
        /// Passes through the object Tag from the option
        /// </summary>
        public IUntypedSignal BalloonOptionClicked { get; private set; }
    }
}