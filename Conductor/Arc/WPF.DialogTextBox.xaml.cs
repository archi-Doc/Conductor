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
    public delegate bool CheckTextDelegate(ref string text); // Delegate to validate text. true:valid, false:invalid.

    public delegate Task<string> CheckTextAsyncDelegate(string text); // Asynchronous version. null:invalid non-null:valid.

    public struct DialogTextBoxParam
    { // parameter
        public string C4Name; // 1st: C4Name
        public string Message; // 2nd: Message
        public MessageBoxButton Button;
        public MessageBoxResult Result;
        public string Text;
        public int TextMaxLength; // max lengh. 0=no limit
        public CheckTextDelegate CheckText;
        public CheckTextAsyncDelegate CheckTextAsync;  // public TaskCompletionSource<DialogStringResult> TCS;
    }

    public struct DialogTextBoxResult
    { // result
        public string Text;
        public MessageBoxResult Result;

        public DialogTextBoxResult(string text, MessageBoxResult result)
        {
            this.Text = text;
            this.Result = result;
        }
    }

    /// <summary>
    /// DialogBox with textbox.
    /// </summary>
    public partial class DialogTextBox : Window
    {
        private string fMessage = string.Empty;
        private MessageBoxButton fButton = MessageBoxButton.OK;
        private MessageBoxResult fResult = MessageBoxResult.None; // focused button and dialog result.

        public string Message
        {
            get { return this.fMessage; } set { this.fMessage = value; }
        }

        public MessageBoxButton Button
        {
            get { return this.fButton; } set { this.fButton = value; }
        }

        public MessageBoxResult Result
        {
            get { return this.fResult; } set { this.fResult = value; }
        }

        public TextBlock TextBlock
        {
            get { return this.PART_TextBlock; }
        }

        public string Text { get; private set; }

        public CheckTextDelegate CheckText { get; private set; }

        public CheckTextAsyncDelegate CheckTextAsync { get; private set; }

        private string captionOK;
        private string captionCancel;
        private string captionYes;
        private string captionNo;

        public DialogTextBox(Window owner, DialogTextBoxParam p)
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
            if (p.TextMaxLength != 0)
            {
                this.textBox.MaxLength = p.TextMaxLength;
            }

            // visual
            this.Foreground = Brushes.DarkBlue;

            // set param
            if (p.C4Name != null)
            {
                this.fMessage = C4.Instance.Get(p.C4Name) ?? string.Empty;
            }

            if (this.fMessage == null || this.fMessage == string.Empty)
            {
                this.fMessage = p.Message;
            }

            if (this.fMessage == null)
            {
                this.fMessage = string.Empty;
            }

            this.fButton = p.Button;
            this.fResult = p.Result;
            this.Text = p.Text;
            this.CheckText = p.CheckText;
            this.CheckTextAsync = p.CheckTextAsync;

            this.textBox.Text = this.Text;
            if (this.PART_TextBlock.Inlines.Count < 1)
            {
                this.PART_TextBlock.Text = this.fMessage;
            }

            this.SetupButton();

            Transformer.Instance.Register(this);
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

        private async Task<bool> Process()
        { // true: success, false: fail
            string t = this.textBox.Text;
            bool flag = true;

            if (this.CheckText != null)
            {
                flag = this.CheckText(ref t); // validate.
            }

            if (this.CheckTextAsync != null)
            {
                t = await this.CheckTextAsync(t); // validate async
            }

            if (t != null)
            {
                this.textBox.Text = t; // Set textbox.
            }

            if (!flag || t == null)
            { // fail
                this.FocusTextBox();
                return false;
            }

            this.Text = t;
            return true;
        }

        private void FocusTextBox()
        { // focus on the text box
            Keyboard.Focus(this.textBox);
            this.textBox.SelectAll();
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            Button? button = sender as Button;
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

            if (this.fResult == MessageBoxResult.OK || this.fResult == MessageBoxResult.Yes)
            { // check
                bool result = await this.Process();
                if (!result)
                {
                    return;
                }
            }

            this.Close();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            { // escape key
                if (this.fButton == MessageBoxButton.OK)
                {// ok -> ok
                    this.fResult = MessageBoxResult.OK;
                    this.Close();
                }
                else if (this.fButton == MessageBoxButton.OKCancel)
                {// ok cancel -> cancel
                    this.fResult = MessageBoxResult.Cancel;
                    this.Close();
                }
                else if (this.fButton == MessageBoxButton.YesNo)
                {// yes no -> wait
                }
                else if (this.fButton == MessageBoxButton.YesNoCancel)
                {// yes no cancel -> cancel
                    this.fResult = MessageBoxResult.Cancel;
                    this.Close();
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (this.fButton == MessageBoxButton.OK)
                {// ok -> ok
                    this.fResult = MessageBoxResult.OK;
                }
                else if (this.fButton == MessageBoxButton.OKCancel)
                {// ok cancel -> ok
                    this.fResult = MessageBoxResult.OK;
                }
                else if (this.fButton == MessageBoxButton.YesNo)
                {// yes no -> yes
                    this.fResult = MessageBoxResult.Yes;
                }
                else if (this.fButton == MessageBoxButton.YesNoCancel)
                {// yes no cancel -> yes
                    this.fResult = MessageBoxResult.Yes;
                }

                bool result = await this.Process();
                if (result)
                {
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
            button.Margin = new Thickness(0, 12, 6, 12);

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.FocusTextBox();
        }
    }
}
