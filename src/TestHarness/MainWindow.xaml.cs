using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Lifetimes;
using JetBrains.UI.StdApplicationUI;
using JetBrains.UI.Utils;
using JetBrains.UI.Wpf;
using JetBrains.Util;
using JetBrains.Util.Interop;
using MessageBox = JetBrains.Util.MessageBox;

namespace TestHarness
{
    public partial class MainWindow
    {
        private static readonly Random Rand = new Random();

        private readonly SequentialLifetimes lifetimes;
        private readonly Lifetime lifetime;
        private Agent agent;
        private bool firstTime;

        public MainWindow()
        {
            InitializeComponent();

            lifetime = Lifetime.Eternal;
            lifetimes = new SequentialLifetimes(lifetime);

            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var agentManager =
                new AgentManager(lifetime, new StaticMainWindow(lifetime, new WpfWin32Window(this)), null);
            agentManager.Initialise();

            // Note, using this lifetime means we get alerts for ALL balloons,
            // not just the current one (i.e. other classes could also create
            // a balloon, and we'd get those clicks, too)
            agent = new Agent(lifetime, agentManager);
            agent.BalloonOptionClicked.Advise(lifetime,
                tag => MessageBox.ShowExclamation(string.Format("Clicked: {0}", tag)));
            agent.ButtonClicked.Advise(lifetime,
                button => MessageBox.ShowExclamation(string.Format("Clicked button: {0}", button)));

            var character = agent.AgentCharacter.Character;
            Animations.ItemsSource = character.Animations.OrderBy();
            Animations.SelectedIndex = 0;

            firstTime = true;
        }

        private void ShowHide(object sender, RoutedEventArgs e)
        {
            if (firstTime)
            {
                var dpiResolution = DpiResolutions.FromAvalonElement(this);
                var x = ((Left + Width)*(dpiResolution.DpiX/DpiResolution.DeviceIndependent96DpiValue))-200;
                var y = ((Top + Height)*(dpiResolution.DpiY/DpiResolution.DeviceIndependent96DpiValue))-200;
                agent.SetLocation(x, y);
                firstTime = false;
            }
            if (agent.IsVisible)
                agent.Hide();
            else
                agent.Show();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            agent.Hide();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }

        private IList<BalloonOption> GetOptionsList()
        {
            if (ShowOptionsListCheckBox.IsChecked.HasValue && ShowOptionsListCheckBox.IsChecked.Value)
            {
                int numberOfOptions;
                if (!int.TryParse(NumberOfOptions.Text, out numberOfOptions))
                    numberOfOptions = 8;
                var options = new List<BalloonOption>();
                for (var i = 0; i < numberOfOptions; i++)
                {
                    var text = LoremIpsum(3, 10);
                    var tag = text; // In real life, something useful
                    var enabled = i != 3;
                    options.Add(new BalloonOption(text, false, enabled, tag));
                }
                return options;
            }

            return EmptyList<BalloonOption>.InstanceList;
        }

        private void ShowBalloon(string header, string message, IList<BalloonOption> options, params string[] buttons)
        {
            lifetimes.Next(balloonLifetime =>
            {
                var activate = ShowActivated.IsChecked.HasValue && ShowActivated.IsChecked.Value;
                agent.ShowBalloon(balloonLifetime, header, message, options, buttons, activate, _ => { });
            });
        }

        private void Speak(object sender, RoutedEventArgs e)
        {
            ShowBalloon(LoremIpsum(2,5), LoremIpsum(4, 15), GetOptionsList(), new string[0]);
        }

        private void SpeakOk(object sender, RoutedEventArgs e)
        {
            ShowBalloon(LoremIpsum(2, 5), LoremIpsum(4, 15), GetOptionsList(), "OK");
        }

        private void SpeakYesNo(object sender, RoutedEventArgs e)
        {
            ShowBalloon(LoremIpsum(2, 5), LoremIpsum(4, 15), GetOptionsList(), "Yes", "No");
        }

        private void SpeakOkCancel(object sender, RoutedEventArgs e)
        {
            ShowBalloon(LoremIpsum(2, 5), LoremIpsum(4, 15), GetOptionsList(), "OK", "Cancel");
        }

        private void SpeakThreeButtons(object sender, RoutedEventArgs e)
        {
            ShowBalloon(LoremIpsum(2, 5), LoremIpsum(4, 25), GetOptionsList(), "Yes", "Maybe", "maybe not", "No");
        }

        private void HideBalloon(object sender, RoutedEventArgs e)
        {
            lifetimes.TerminateCurrent();
        }

        // I love StackOverflow - http://stackoverflow.com/questions/4286487/is-there-any-lorem-ipsum-generator-in-c
        static string LoremIpsum(int minWords, int maxWords, int minSentences = 1, int maxSentences = 1, int numParagraphs = 1)
        {
            var words = new[]
            {
                "lo_rem", "ipsum", "dolor", "sit", "amet", "con_sectetuer",
                "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
                "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
            };

            var result = string.Empty;
            for (var p = 0; p < numParagraphs; p++)
            {
                for (var s = 0; s < Rand.Next(maxSentences - minSentences) + minSentences; s++)
                {
                    if (s > 0) result += ". ";
                    for (var w = 0; w < Rand.Next(maxWords - minWords) + minWords + 1; w++)
                    {
                        if (w > 0) { result += " "; }
                        result += words[Rand.Next(words.Length)];
                    }
                }
            }

            return result;
        }

        private void Animate(object sender, RoutedEventArgs e)
        {
            var animation = Animations.SelectedItem as string;
            if (string.IsNullOrEmpty(animation))
                return;

            agent.Play(animation);
        }
    }
}
