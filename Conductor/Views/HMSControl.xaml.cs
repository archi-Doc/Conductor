// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Conductor.ViewModels;

#pragma warning disable SA1649 // File name should match first type name

namespace Conductor.Views
{
    public partial class HMSControl : UserControl
    {
        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register("EnterCommand", typeof(ICommand), typeof(HMSControl), new PropertyMetadata(null));

        public HMSControl()
        {
            this.InitializeComponent();
        }

        public ICommand EnterCommand
        {
            get => (ICommand)this.GetValue(EnterCommandProperty);

            set => this.SetValue(EnterCommandProperty, value);
        }

        private bool IsOnlyDigits(ReadOnlySpan<char> str)
        {
            foreach (var x in str)
            {
                if (x >= '0' && x <= '9')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void HourTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !this.IsOnlyDigits(e.Text);
            return;
        }

        private void MinuteTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !this.IsOnlyDigits(e.Text);
            return;
        }

        private void SecondTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !this.IsOnlyDigits(e.Text);
            return;
        }

        private void HourTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = this.DataContext as HMSControlVM;

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                vm?.UpDown(0, true);
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                vm?.UpDown(0, false);
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.EnterCommand?.Execute(null);
            }

            return;
        }

        private void MinuteTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = this.DataContext as HMSControlVM;

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                vm?.UpDown(1, true);
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                vm?.UpDown(1, false);
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.EnterCommand?.Execute(null);
            }
        }

        private void SecondTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = this.DataContext as HMSControlVM;

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                vm?.UpDown(2, true);
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                vm?.UpDown(2, false);
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.EnterCommand?.Execute(null);
            }
        }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void TextBox_TextChanged_Limit60(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (int.TryParse(tb.Text, out var result))
                {
                    if (result >= 60)
                    {
                        tb.Text = "59";
                    }
                }
            }
        }

        private void HourTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vm = this.DataContext as HMSControlVM;

            if (e.Delta > 0)
            {
                e.Handled = true;
                vm?.UpDown(0, true);
            }
            else if (e.Delta < 0)
            {
                e.Handled = true;
                vm?.UpDown(0, false);
            }
        }

        private void MinuteTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vm = this.DataContext as HMSControlVM;

            if (e.Delta > 0)
            {
                e.Handled = true;
                vm?.UpDown(1, true);
            }
            else if (e.Delta < 0)
            {
                e.Handled = true;
                vm?.UpDown(1, false);
            }
        }

        private void SecondTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vm = this.DataContext as HMSControlVM;

            if (e.Delta > 0)
            {
                e.Handled = true;
                vm?.UpDown(2, true);
            }
            else if (e.Delta < 0)
            {
                e.Handled = true;
                vm?.UpDown(2, false);
            }
        }
    }
}
