using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.DataFlow;
using JetBrains.Util;
using JetBrains.Util.Interop;
using MessageBox = JetBrains.Util.MessageBox;

namespace TestHarness
{
    public partial class MainWindow
    {
        private static readonly Random Rand = new Random();
        private readonly Agent agent;
        private bool firstTime;

        public MainWindow()
        {
            InitializeComponent();

            var lifetime = EternalLifetime.Instance;

            var agentManager = new AgentManager(lifetime);
            agentManager.Initialise();

            agent = new Agent(lifetime, agentManager);
            agent.BalloonOptionClicked.Advise(lifetime,
                tag => MessageBox.ShowExclamation(string.Format("Clicked: {0}", tag)));
            agent.ButtonClicked.Advise(lifetime,
                button => MessageBox.ShowExclamation(string.Format("Clicked button: {0}", button)));
            firstTime = true;
        }

        private void ShowHide(object sender, RoutedEventArgs e)
        {
            if (firstTime)
            {
                var x = ((Left + Width)*DpiUtil.DpiHorizontalFactor)-200;
                var y = ((Top + Height)*DpiUtil.DpiVerticalFactor)-200;
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
                    options.Add(new BalloonOption(text, false, tag));
                }
                return options;
            }

            return EmptyList<BalloonOption>.InstanceList;
        }

        private void Speak(object sender, RoutedEventArgs e)
        {
            agent.Say(LoremIpsum(2,5), LoremIpsum(4, 15), GetOptionsList(), _ => { });
        }

        private void SpeakOk(object sender, RoutedEventArgs e)
        {
            agent.Tell(LoremIpsum(2, 5), LoremIpsum(4, 15), GetOptionsList(), _ => { });
        }

        private void SpeakYesNo(object sender, RoutedEventArgs e)
        {
            agent.AskYesNo(LoremIpsum(2, 5), LoremIpsum(4, 15), GetOptionsList(), _ => { });
        }

        private void SpeakOkCancel(object sender, RoutedEventArgs e)
        {
            agent.AskOkCancel(LoremIpsum(2, 5), LoremIpsum(4, 25), GetOptionsList(), _ => { });
        }

        private void SpeakThreeButtons(object sender, RoutedEventArgs e)
        {
            agent.Ask(LoremIpsum(2, 5), LoremIpsum(4, 25), new[] { "Yes", "Maybe, maybe not", "No" }, GetOptionsList(), _ => { });
        }

        private void Think(object sender, RoutedEventArgs e)
        {
            //agent.Think("Hello world");
        }

        private void HideBalloon(object sender, RoutedEventArgs e)
        {
            agent.HideBalloon();
        }

        // I love StackOverflow - http://stackoverflow.com/questions/4286487/is-there-any-lorem-ipsum-generator-in-c
        static string LoremIpsum(int minWords, int maxWords, int minSentences = 1, int maxSentences = 1, int numParagraphs = 1)
        {
            var words = new[]
            {
                "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
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
    }
}
