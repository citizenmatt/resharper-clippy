using System;
using System.Collections.Generic;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon;
using JetBrains.DataFlow;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    public class BalloonManager
    {
        private readonly SequentialLifetimes balloonLifetimes;
        private BalloonWindow balloonWindow;

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

                // If the client wants to hide the balloon, they can terminate clientLifetime
                // If another client calls CreateNew, balloonLifetimes.Next terminates
                // balloonLifetime. Whichever lifetime terminates first will cause
                // combinedLifetime to terminate, closing the window. 
                var combinedLifetime = Lifetimes.CreateIntersection2(clientLifetime, balloonLifetime).Lifetime;
                combinedLifetime.AddAction(() =>
                {
                    if (balloonWindow != null)
                    {
                        balloonWindow.Close();
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

        public void Show(short left, short top, short width, short height)
        {
            balloonWindow.Show(left, top, width, height);
        }

        public void UpdateTargetPosition(short x, short y)
        {
            if (balloonWindow != null)
                balloonWindow.UpdateTargetPosition(x, y);
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