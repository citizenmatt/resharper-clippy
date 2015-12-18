using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using JetBrains.Util;
using TextBoxBase = System.Windows.Controls.Primitives.TextBoxBase;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public partial class BalloonWindow : INotifyPropertyChanged
    {
        private const int PageSize = 5;

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof (string), typeof (BalloonWindow), new PropertyMetadata("What would you like to do?"));

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            "Message", typeof (string), typeof (BalloonWindow), new PropertyMetadata(default(string)));

        private static readonly DependencyProperty ButtonsProperty = DependencyProperty.Register(
            "Buttons", typeof (ObservableCollection<Indexed<string>>), typeof (BalloonWindow),
            new PropertyMetadata(default(ObservableCollection<Indexed<string>>)));

        private static readonly DependencyProperty OptionsPageProperty = DependencyProperty.Register(
            "OptionsPage", typeof (ObservableCollection<Indexed<BalloonOption>>), typeof (BalloonWindow),
            new PropertyMetadata(default(ObservableCollection<Indexed<BalloonOption>>)));

        public static readonly DependencyProperty ShowSearchProperty = DependencyProperty.Register(
            "ShowSearch", typeof (bool), typeof (BalloonWindow), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty HasButtonsProperty = DependencyProperty.Register(
            "HasButtons", typeof (bool), typeof (BalloonWindow), new PropertyMetadata(default(bool)));

        private int currentPage;
        private IList<BalloonOption> allOptions;

        public BalloonWindow()
        {
            InitializeComponent();

            DataContext = this;

            OptionsPage = new ObservableCollection<Indexed<BalloonOption>>();
            Buttons = new ObservableCollection<Indexed<string>>();
        }

        public event EventHandler<BalloonActionEventArgs<string>> ButtonClicked;
        public event EventHandler<BalloonActionEventArgs<object>> OptionClicked;

        public string Header
        {
            get { return (string) GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public string Message
        {
            get { return (string) GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        private ObservableCollection<Indexed<string>> Buttons
        {
            get { return (ObservableCollection<Indexed<string>>) GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

        private ObservableCollection<Indexed<BalloonOption>> OptionsPage
        {
            get { return (ObservableCollection<Indexed<BalloonOption>>) GetValue(OptionsPageProperty); }
            set { SetValue(OptionsPageProperty, value); }
        }

        public bool ShowSearch
        {
            get { return (bool) GetValue(ShowSearchProperty); }
            set { SetValue(ShowSearchProperty, value); }
        }

        public bool HasButtons
        {
            get { return (bool) GetValue(HasButtonsProperty); }
            set { SetValue(HasButtonsProperty, value); }
        }

        private void ExecutedShowPreviousCommand(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            currentPage--;
            UpdateOptionsPage();
        }

        private void ExecutedShowNextCommand(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            currentPage++;
            UpdateOptionsPage();
        }

        private void UpdateOptionsPage()
        {
            OptionsPage.Clear();
            OptionsPage.AddRange(
                allOptions.Skip(currentPage*PageSize).Take(PageSize).Select((o, i) => new Indexed<BalloonOption>(i, o)));

            ShowPreviousButton = allOptions != null && currentPage > 0;
            ShowNextButton = allOptions != null && ((currentPage + 1)*PageSize) < allOptions.Count;
        }

        private bool showPreviousButton;
        private bool showNextButton;

        public bool ShowPreviousButton
        {
            get { return showPreviousButton; }
            set
            {
                if (value != showPreviousButton)
                {
                    showPreviousButton = value;
                    OnPropertyChanged("ShowPreviousButton");
                }
            }
        }

        public bool ShowNextButton
        {
            get { return showNextButton; }
            set
            {
                if (value != showNextButton)
                {
                    showNextButton = value;
                    OnPropertyChanged("ShowNextButton");
                }
            }
        }

        public void SetOptions(IList<BalloonOption> options)
        {
            currentPage = 0;
            allOptions = options ?? EmptyList<BalloonOption>.InstanceList;
            UpdateOptionsPage();
        }

        public void SetButtons(IEnumerable<string> buttons)
        {
            Buttons.Clear();
            Buttons.AddRange(buttons.Select((b, i) => new Indexed<string>(i, b)));
            HasButtons = Buttons.Any();
        }

        private void OnSearchTextBoxGetFocus(object sender, RoutedEventArgs e)
        {
            ((TextBoxBase) sender).SelectAll();
        }

        private void ExecutedOptionCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(e.Parameter is int))
                return;

            var index = ((int)e.Parameter) + (currentPage * PageSize);
            var handler = OptionClicked;
            if (handler != null)
                handler(this, new BalloonActionEventArgs<object>(index, allOptions[index].Tag));
        }

        private void ExecutedButtonCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(e.Parameter is Indexed<string>))
                return;

            var index = (Indexed<string>)e.Parameter;
            var handler = ButtonClicked;
            if (handler != null)
                handler(this, new BalloonActionEventArgs<string>(index.Index, index.Value));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
