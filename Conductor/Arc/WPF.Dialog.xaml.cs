// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arc.Text;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.WPF
{
    public struct DialogParam
    { // Dialog Parameter
        public string C4Name; // 1st: C4Name
        public string Message; // 2nd: Message
        public MessageBoxButton Button;
        public MessageBoxImage Image;
        public MessageBoxResult Result; // public TaskCompletionSource<MessageBoxResult> TCS;
    }

    /// <summary>
    /// Dialog class.
    /// </summary>
    public partial class Dialog : Window
    {
        private string fMessage = string.Empty; // message.
        private MessageBoxButton fButton = MessageBoxButton.OK; // button.
        private MessageBoxImage fImage = MessageBoxImage.Information; // icon.
        private MessageBoxResult fResult = MessageBoxResult.None; // focused button and dialog result.

        public string Message
        {
            get { return this.fMessage; }
            set { this.fMessage = value; }
        }

        public MessageBoxButton Button
        {
            get { return this.fButton; }
            set { this.fButton = value; }
        }

        public MessageBoxImage Image
        {
            get { return this.fImage; }
            set { this.fImage = value; }
        }

        public MessageBoxResult Result
        {
            get { return this.fResult; }
            set { this.fResult = value; }
        }

        public TextBlock TextBlock
        {
            get { return this.PART_TextBlock; }
        }

        private string captionOK;
        private string captionCancel;
        private string captionYes;
        private string captionNo;

        public Dialog(Window owner)
        {
            this.InitializeComponent();

            this.captionOK = C4.Instance.Get("dialog.ok") ?? "O K";
            this.captionCancel = C4.Instance.Get("dialog.cancel") ?? "Cancel";
            this.captionYes = C4.Instance.Get("dialog.yes") ?? "Yes";
            this.captionNo = C4.Instance.Get("dialog.no") ?? "No";

            // settings
            this.FontSize = owner.FontSize;
            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.ShowInTaskbar = false;

            // visual
            this.Foreground = Brushes.DarkBlue;

            Transformer.Instance.Register(this);
        }

        public Dialog(Window owner, DialogParam p)
            : this(owner)
        {
            if (p.C4Name != null)
            {
                this.fMessage = C4.Instance.Get(p.C4Name) ?? string.Empty;
            }

            if (this.fMessage == string.Empty)
            {
                this.fMessage = p.Message;
            }

            this.fButton = p.Button;
            this.fImage = p.Image;
            if (this.fImage == MessageBoxImage.None)
            {
                this.fImage = MessageBoxImage.Information;
            }

            this.fResult = p.Result;
        }

        public Task<MessageBoxResult> ShowDialogAsync()
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();
            this.Dispatcher.InvokeAsync(new Action(() =>
            {
                this.ShowDialog();
                tcs.SetResult(this.Result);
            }));
            return tcs.Task;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.PART_TextBlock.Inlines.Count < 1)
            {
                this.PART_TextBlock.Text = this.fMessage;
            }

            this.SetupButtonImage();
            this.SetupButton();

            Arc.WinAPI.Methods.SendKey(Arc.WinAPI.VirtualKeyCode.DOWN); // down arrow
            Arc.WinAPI.Methods.SendKey(Arc.WinAPI.VirtualKeyCode.UP); // up arrow
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        // Setup Buttons.
        private void SetupButton()
        {
            if (this.fButton == MessageBoxButton.OK)
            {
                this.CreateButton("btnOK", this.captionOK);
            }
            else if (this.fButton == MessageBoxButton.OKCancel)
            {
                this.CreateButton("btnOK", this.captionOK);
                this.CreateButton("btnCancel", this.captionCancel);
            }
            else if (this.fButton == MessageBoxButton.YesNo)
            {
                this.CreateButton("btnYes", this.captionYes);
                this.CreateButton("btnNo", this.captionNo);
            }
            else if (this.fButton == MessageBoxButton.YesNoCancel)
            {
                this.CreateButton("btnYes", this.captionYes);
                this.CreateButton("btnNo", this.captionNo);
                this.CreateButton("btnCancel", this.captionCancel);
            }

            // right margin.
            var border = new Border();
            border.Width = 10;
            this.PART_StackPanel.Children.Add(border);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            if (button.Name == "btnOK")
            {
                this.fResult = MessageBoxResult.OK;
            }
            else if (button.Name == "btnCancel")
            {
                this.fResult = MessageBoxResult.Cancel;
            }
            else if (button.Name == "btnYes")
            {
                this.fResult = MessageBoxResult.Yes;
            }
            else if (button.Name == "btnNo")
            {
                this.fResult = MessageBoxResult.No;
            }

            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            { // escape key
                if (this.fButton == MessageBoxButton.OK)
                { // ok -> ok
                    this.fResult = MessageBoxResult.OK;
                    this.Close();
                }
                else if (this.fButton == MessageBoxButton.OKCancel)
                { // ok cancel -> cancel
                    this.fResult = MessageBoxResult.Cancel;
                    this.Close();
                }
                else if (this.fButton == MessageBoxButton.YesNo)
                { // yes no -> wait
                }
                else if (this.fButton == MessageBoxButton.YesNoCancel)
                { // yes no cancel -> cancel
                    this.fResult = MessageBoxResult.Cancel;
                    this.Close();
                }
            }
        }

        // Create button and set focus on specified button (fResult).
        private void CreateButton(string name, string caption)
        {
            var button = new Button();
            button.Name = name;
            button.Width = 80;
            button.Content = caption;
            button.Margin = new Thickness(0, 10, 6, 10);

            button.Click += new RoutedEventHandler(this.Button_Click);

            this.PART_StackPanel.Children.Add(button);

            if (this.fResult == MessageBoxResult.None)
            {
                if ((name == "btnOK") || (name == "btnYes"))
                {
                    Keyboard.Focus(button);
                }
            }
            else if (this.fResult == MessageBoxResult.OK)
            {
                if (name == "btnOK")
                {
                    Keyboard.Focus(button);
                }
            }
            else if (this.fResult == MessageBoxResult.Cancel)
            {
                if (name == "btnCancel")
                {
                    Keyboard.Focus(button);
                }
            }
            else if (this.fResult == MessageBoxResult.Yes)
            {
                if (name == "btnYes")
                {
                    Keyboard.Focus(button);
                }
            }
            else if (this.fResult == MessageBoxResult.No)
            {
                if (name == "btnNo")
                {
                    Keyboard.Focus(button);
                }
            }
        }

        private void SetupButtonImage()
        {
            StockIconId id = StockIconId.SIID_INFO;

            switch (this.fImage)
            {
                case MessageBoxImage.Stop:
                    // case MessageBoxImage.Hand:
                    // case MessageBoxImage.Error:
                    id = StockIconId.SIID_ERROR;
                    break;
                case MessageBoxImage.Question:
                    id = StockIconId.SIID_HELP;
                    break;
                case MessageBoxImage.Exclamation:
                    // case MessageBoxImage.Warning:
                    id = StockIconId.SIID_WARNING;
                    break;
            }

            this.PART_Image.Source = this.GetStockIconById(id, StockIconFlags.Large);
        }

        [DllImport("User32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("Shell32.dll")]
        private static extern IntPtr SHGetStockIconInfo(StockIconId siid, StockIconFlags uFlags, ref StockIconInfo psii);

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StockIconInfo
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysImageIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [Flags]
        private enum StockIconFlags
        {
            Large = 0x000000000,
            Small = 0x000000001,
            ShellSize = 0x000000004,
            Handle = 0x000000100,
            SystemIndex = 0x000004000,
            LinkOverlay = 0x000008000,
            Selected = 0x000010000,
        }

        private enum StockIconId
        {
            SIID_HELP = 23,
            SIID_WARNING = 78,
            SIID_INFO = 79,
            SIID_ERROR = 80,
        }

        private BitmapSource? GetStockIconById(StockIconId id, StockIconFlags flag)
        {
            BitmapSource? bitmapSource = null;
            StockIconFlags flags = StockIconFlags.Handle | flag;

            var info = default(StockIconInfo);
            info.cbSize = (uint)Marshal.SizeOf(typeof(StockIconInfo));

            IntPtr result = SHGetStockIconInfo(id, flags, ref info);

            if (info.hIcon != IntPtr.Zero)
            {
                bitmapSource = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, null);
                DestroyIcon(info.hIcon);
            }

            return bitmapSource;
        }
    }
}
