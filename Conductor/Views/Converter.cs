// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using Application;

#pragma warning disable SA1649 // File name should match first type name

namespace Conductor.Views
{
    public class DisplayScalingToStringConverter : IValueConverter
    {// Display scaling to String
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double d)
            {
                return (d * 100).ToString("F0") + "%";
            }
#if XAMARIN
            return null;;
#else
            return System.Windows.DependencyProperty.UnsetValue;
#endif
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double d = 1;

            if (value is string st)
            {
                d = double.Parse(st.TrimEnd(new char[] { '%', ' ' })) / 100;
            }

            return d;
        }
    }

    public class CultureToStringConverter : IValueConverter
    {// Culture to String
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value is string)
            {
                switch ((string)value)
                {
                    case "en": // eglish
                        return App.C4["language.en"];
                    case "ja": // japanese
                        return App.C4["language.ja"];

                    default: // default = english
                        return App.C4["language.en"];
                }
            }
#if XAMARIN
            return null;;
#else
            return System.Windows.DependencyProperty.UnsetValue;
#endif
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = false;

            if (value is bool)
            {
                b = (bool)value;
            }
            else if (value is bool?)
            {
                var b2 = (bool?)value;
                b = b2.HasValue ? b2.Value : false;
            }

            if (parameter != null)
            { // Reverse conversion on any given parameter.
                b = !b;
            }

            return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b;

            if (value is System.Windows.Visibility v)
            {
                b = v == System.Windows.Visibility.Visible;
            }
            else
            {
                b = false;
            }

            if (parameter != null)
            { // Reverse conversion on any given parameter.
                b = !b;
            }

            return b;
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = false;

            if (value is bool)
            {
                b = (bool)value;
            }
            else if (value is bool?)
            {
                var b2 = (bool?)value;
                b = b2.HasValue ? b2.Value : false;
            }

            return !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b;

            if (value is bool b2)
            {
                b = b2;
            }
            else
            {
                b = false;
            }

            return !b;
        }
    }
}
