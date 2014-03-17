using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using JetBrains.Util;
using JetBrains.Util.Interop;
using Point = System.Drawing.Point;
using TextBoxBase = System.Windows.Controls.Primitives.TextBoxBase;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public partial class BalloonWindow
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

        private Rect targetBounds;
        private Screen currentScreen;
        private Rect currentScreenBounds;
        private int currentPage;
        private IList<BalloonOption> allOptions;

        public BalloonWindow()
        {
            InitializeComponent();

            LayoutUpdated += OnLayoutUpdated;
            IsVisibleChanged += OnVisibleChanged;

            DataContext = this;
            UpdateScreen();

            CreateTargetBoundsWindow();

            OptionsPage = new ObservableCollection<Indexed<BalloonOption>>();
            Buttons = new ObservableCollection<Indexed<string>>();
        }

        // Debugging, for placement of balloon
        partial void CreateTargetBoundsWindow();
        partial void UpdateTargetBoundsWindow();
        partial void HideTargetBoundsWindow();

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

        private void OnCanExecuteShowPreviousCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = allOptions != null && currentPage > 0;
        }

        private void ExecutedShowPreviousCommand(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            currentPage--;
            UpdateOptionsPage();
        }

        private void OnCanExecuteShowNextCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = allOptions != null && ((currentPage + 1)*PageSize) < allOptions.Count;
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

            CommandManager.InvalidateRequerySuggested();
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

        private void SetTargetBounds(short x, short y, short w, short h)
        {
            targetBounds = new Rect(x/DpiUtil.DpiHorizontalFactor, y/DpiUtil.DpiVerticalFactor,
                w/DpiUtil.DpiHorizontalFactor, h/DpiUtil.DpiVerticalFactor);
            UpdateTargetPosition(x, y);
        }

        public void UpdateTargetPosition(short x, short y)
        {
            targetBounds.X = x/DpiUtil.DpiHorizontalFactor;
            targetBounds.Y = y/DpiUtil.DpiVerticalFactor;

            UpdateScreen();
            UpdateTargetBoundsWindow();

            SetBalloonPosition();
        }

        private void SetPointerPosition(string newPosition)
        {
            if (Balloon.PointerPosition != newPosition)
            {
                if (Balloon.PointerPosition == "Bottom")
                    Height -= Balloon.PointerLength;
                else
                    Width -= Balloon.PointerLength;

                MinHeight = 0;
                MinWidth = 0;
            }

            Balloon.PointerPosition = newPosition;
        }

        private void SetBalloonPosition()
        {
            var screen = currentScreenBounds;

            if (double.IsNaN(Height) || double.IsNaN(Width))
                return;

            if (CanBalloonFitOnTopOfTarget())
            {
                SetPointerPosition("Bottom");
                Top = targetBounds.Top - Height;
                var left = targetBounds.Left - ((Width - targetBounds.Width)/2.0);
                left = Math.Max(left, screen.Left);
                Left = Math.Min(left, screen.Right - Width);
            }
            else if (CanBalloonFitOnRightOfTarget())
            {
                SetPointerPosition("Left");
                var top = targetBounds.Top - ((Height - targetBounds.Height)/2.0);
                top = Math.Max(top, screen.Top);
                Top = Math.Min(top, screen.Bottom - Height);
                Left = targetBounds.Right;
            }
            else
            {
                SetPointerPosition("Right");
                var top = targetBounds.Top - ((Height - targetBounds.Height)/2.0);
                top = Math.Max(top, screen.Top);
                Top = Math.Min(top, screen.Bottom - Height);
                Left = targetBounds.Left - Width;
            }

            MinHeight = Math.Max(MinHeight, Height);
            MinWidth = Math.Max(MinWidth, Width);
        }

        private bool CanBalloonFitOnTopOfTarget()
        {
            // When pointer is at bottom, Height includes the pointer length, when it's
            // to the side, it doesn't, and we can get a false positive that the balloon
            // will fit, and it moves, changes the pointer position and no longer fits.
            // Flicker-tastic
            var heightWithPointer = Balloon.PointerPosition == "Bottom" ? Height : Height + Balloon.PointerLength;
            return targetBounds.Top - heightWithPointer > currentScreenBounds.Top;
        }

        private bool CanBalloonFitOnRightOfTarget()
        {
            return targetBounds.Right + Width < currentScreenBounds.Right;
        }


        public void Show(short x, short y, short w, short h, bool activate)
        {
            SetTargetBounds(x, y, w, h);

            ShowActivated = activate;

            Show();
        }

        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            if (!double.IsNaN(Height) && !double.IsNaN(Width))
            {
                SetBalloonPosition();
            }
        }

        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
                HideTargetBoundsWindow();
        }

        private void UpdateScreen()
        {
            var translatedPoint = new Point((int) (targetBounds.X / DpiUtil.DpiHorizontalFactor),
                (int) (targetBounds.Y / DpiUtil.DpiVerticalFactor));

            if (currentScreen != null && !currentScreen.Bounds.Contains(translatedPoint))
                currentScreen = null;

            currentScreen = Screen.FromPoint(translatedPoint);
            currentScreenBounds = new Rect(currentScreen.Bounds.Left / DpiUtil.DpiHorizontalFactor,
                currentScreen.Bounds.Top / DpiUtil.DpiVerticalFactor,
                currentScreen.Bounds.Width / DpiUtil.DpiHorizontalFactor,
                currentScreen.Bounds.Height / DpiUtil.DpiVerticalFactor);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            HideTargetBoundsWindow();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            HideTargetBoundsWindow();
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
    }
}
