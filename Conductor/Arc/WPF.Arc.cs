// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Arc.Text;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1618 // Generic type parameters should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.WPF
{
    public static class TextBoxAttachment
    {
        public static readonly DependencyProperty IsSelectAllOnGotFocusProperty =
            DependencyProperty.RegisterAttached("IsSelectAllOnGotFocus", typeof(bool), typeof(TextBoxAttachment), new PropertyMetadata(false, (d, e) =>
            {
                if (!(d is TextBox tb))
                {
                    return;
                }

                if (!(e.NewValue is bool isSelectAll))
                {
                    return;
                }

                tb.GotFocus -= OnTextBoxGotFocus;
                tb.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
                if (isSelectAll)
                {
                    tb.GotFocus += OnTextBoxGotFocus;
                    tb.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                }
            }));

        public static bool GetIsSelectAllOnGotFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSelectAllOnGotFocusProperty);
        }

        public static void SetIsSelectAllOnGotFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSelectAllOnGotFocusProperty, value);
        }

        private static void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                var isSelectAllOnGotFocus = GetIsSelectAllOnGotFocus(tb);

                if (isSelectAllOnGotFocus)
                {
                    tb.SelectAll();
                }
            }
        }

        private static void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.IsFocused)
                {
                    return;
                }

                tb.Focus();
                e.Handled = true;
            }
        }
    }

    public class C4FormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var format = C4.Instance[(string)parameter];
            if (format == null)
            {
                return "null";
            }

            return string.Format(format, values);
        }

        public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // reverse conversion (false=>Visible, true=>collapsed) on any given parameter
            bool input = !((bool)value);
            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class DependencyObjectExtensions
    {
        // Children - 子要素を取得
        public static IEnumerable<DependencyObject> Children(this DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var count = VisualTreeHelper.GetChildrenCount(obj); // VisualTreeHelper
            if (count == 0)
            {
                yield break;
            }

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null)
                {
                    yield return child;
                }
            }
        }

        // Children - 特定の型の子要素を取得
        public static IEnumerable<T> Children<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.Children().OfType<T>();
        }

        // Descendants - 子孫要素を取得
        public static IEnumerable<DependencyObject> Descendants(this DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            foreach (var child in obj.Children())
            {
                yield return child;
                foreach (var grandChild in child.Descendants())
                {
                    yield return grandChild;
                }
            }
        }

        // Descendants - 特定の型の子孫要素を取得
        public static IEnumerable<T> Descendants<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.Descendants().OfType<T>();
        }

        // LogicalDescendants - 子孫要素を取得
        public static IEnumerable<DependencyObject> LogicalDescendants(this DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            foreach (var child in LogicalTreeHelper.GetChildren(obj))
            {
                var x = child as DependencyObject;
                if (x != null)
                {
                    yield return x;
                    foreach (var y in x.LogicalDescendants())
                    {
                        yield return y;
                    }
                }
            }
        }

        // LogicalDescendants - 特定の型の子孫要素を取得
        public static IEnumerable<T> LogicalDescendants<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.LogicalDescendants().OfType<T>();
        }

        // LogicalChildren - 子要素を取得
        public static IEnumerable<DependencyObject> LogicalChildren(this DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            foreach (var child in LogicalTreeHelper.GetChildren(obj))
            {
                var x = child as DependencyObject;
                if (x != null)
                {
                    yield return x;
                }
            }
        }

        // LogicalChildren - 特定の型の子要素を取得
        public static IEnumerable<T> LogicalChildren<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.LogicalChildren().OfType<T>();
        }

        public static FrameworkElement? FirstLogicalFrameworkElement(this DependencyObject obj)
        {
            foreach (var x in LogicalTreeHelper.GetChildren(obj))
            {
                if (x is FrameworkElement result)
                {
                    return result;
                }
            }

            return null;
        }
    }

    public static class Methods
    {
        /// <summary>
        /// Search the ancestor object corredponding with the specified Type T.
        /// </summary>
        public static T? FindAncestor<T>(DependencyObject dependencyObject)
            where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null)
            {
                return null;
            }

            var parentT = parent as T;
            return parentT ?? FindAncestor<T>(parent);
        }

        /// <summary>
        /// Find the ancestor. True: found, False: not found.
        /// </summary>
        public static bool FindAncestor(DependencyObject targetParent, DependencyObject dependencyObject)
        {
            DependencyObject obj = dependencyObject;
            while (obj != null)
            {
                if (obj == targetParent)
                {
                    return true;
                }

                obj = VisualTreeHelper.GetParent(obj);
            }

            return false; // not found
        }

        /// <summary>
        /// Sort ObservableCollection.
        /// </summary>
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }
    }

    public class ListViewDD : ListView
    { // drag & drop対応のListView
        private ListViewItemDD? dragItem;
        private Point dragStartPos;
        private DragAdorner? dragGhost;

        public Action<int, int>? DropMoveAction { get; set; }

        public ListViewItem? GetItem(Point pos)
        {
            var result = VisualTreeHelper.HitTest(this, pos);
            var item = result?.VisualHit;
            while (item != null)
            {
                if (item is ListViewItemDD)
                {
                    break;
                }

                item = VisualTreeHelper.GetParent(item);
            }

            return item as ListViewItem;
        }

        protected override DependencyObject GetContainerForItemOverride() => new ListViewItemDD();

        protected override bool IsItemItsOwnContainerOverride(object item) => item is ListViewItemDD;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        { // mouse left down
            this.dragItem = this.GetItem(e.GetPosition(this)) as ListViewItemDD; // マウス下のアイテムを取得する
            if (this.dragItem != null)
            {
                // dragIndex = Items.IndexOf(i.Content);
                this.dragStartPos = e.GetPosition(this.dragItem);
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            this.dragItem = null;
            base.OnPreviewMouseLeftButtonUp(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        { // mouse move
            if (e.LeftButton == MouseButtonState.Pressed && this.dragItem != null && this.dragGhost == null)
            {
                var nowPos = e.GetPosition(this.dragItem);
                if (Math.Abs(nowPos.X - this.dragStartPos.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(nowPos.Y - this.dragStartPos.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    this.AllowDrop = true;

                    var layer = AdornerLayer.GetAdornerLayer(this);
                    this.dragGhost = new DragAdorner(this, this.dragItem, 0.5, this.dragStartPos);
                    layer.Add(this.dragGhost);
                    DragDrop.DoDragDrop(this.dragItem, this.dragItem, DragDropEffects.Move);
                    layer.Remove(this.dragGhost);
                    this.dragItem = null;
                    this.dragGhost = null;

                    this.AllowDrop = false;
                }
            }

            base.OnPreviewMouseMove(e);
        }

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        { // drag
            if ((this.dragItem != null) && (this.dragGhost != null))
            { // move ghost
                var p = Arc.WinAPI.Methods.GetNowPosition(this); // var loc = dragItem.PointFromScreen(this.PointToScreen(new Point(0, 0)));
                this.dragGhost.LeftOffset = p.X; // - loc.X;
                this.dragGhost.TopOffset = p.Y; // - loc.Y;
            }

            base.OnQueryContinueDrag(e);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            if (this.dragItem != null)
            { // check dragItem
                var dropPos = e.GetPosition(this);
                var count = this.Items.Count;
                var index = this.Items.IndexOf(this.dragItem.Content);
                if (count >= 2)
                { // 2つ以上の場合のみ
                    for (int i = 0; i < count; i++)
                    {
                        var item = this.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItemDD;
                        if (item == null)
                        {
                            continue;
                        }

                        var pos = this.PointFromScreen(item.PointToScreen(new Point(0, item.ActualHeight / 2)));
                        if (dropPos.Y < pos.Y)
                        {
                            // i が入れ換え先のインデックス
                            this.DropMove(index, (index < i) ? i - 1 : i);
                            goto _OnDropExit;
                        }
                    }

                    // 最後にもっていく
                    this.DropMove(index, count - 1);
                }
            }

_OnDropExit:
            this.dragItem = null; // clear
            base.OnDrop(e);
        }

        private void DropMove(int oldIndex, int newIndex)
        {
            if ((oldIndex != newIndex) && (this.DropMoveAction != null))
            {
                this.DropMoveAction(oldIndex, newIndex);
            }
        }
    }

    public class ListViewItemDD : ListViewItem
    { // drag & drop対応のListViewItem
        protected override void OnMouseEnter(MouseEventArgs e)
        { // マウスドラッグで、選択要素を変更しないようにする。
            var parent = ItemsControl.ItemsControlFromItemContainer(this); // as UIElement, tool.FindAncestor<ListViewMod>(this)
            if (parent.IsMouseCaptured)
            {
                parent.ReleaseMouseCapture();
            }

            base.OnMouseEnter(e);
        }
    }

    public class DragAdorner : Adorner
    { // ghost adorner
        private UIElement child;
        private double xCenter;
        private double yCenter;
        private double leftOffset;
        private double topOffset;

        public DragAdorner(UIElement owner, UIElement adornElement, double opacity, Point dragPos)
            : base(owner)
        {
            var brush = new VisualBrush(adornElement) { Opacity = opacity };
            var b = VisualTreeHelper.GetDescendantBounds(adornElement);
            var r = new Rectangle() { Width = b.Width, Height = b.Height };

            this.xCenter = dragPos.X; // r.Width / 2;
            this.yCenter = dragPos.Y; // r.Height / 2;

            r.Fill = brush;
            this.child = r;
        }

        public double LeftOffset
        {
            get
            {
                return this.leftOffset;
            }

            set
            {
                this.leftOffset = value - this.xCenter;
                this.UpdatePosition();
            }
        }

        public double TopOffset
        {
            get
            {
                return this.topOffset;
            }

            set
            {
                this.topOffset = value - this.yCenter;
                this.UpdatePosition();
            }
        }

        protected override int VisualChildrenCount => 1;

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(this.leftOffset, this.topOffset));
            return result;
        }

        protected override Visual GetVisualChild(int index)
        {
            return this.child;
        }

        protected override Size MeasureOverride(Size finalSize)
        {
            this.child.Measure(finalSize);
            return this.child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            this.child.Arrange(new Rect(this.child.DesiredSize));
            return finalSize;
        }

        private void UpdatePosition()
        {
            var adorner = this.Parent as AdornerLayer;
            if (adorner != null)
            {
                adorner.Update(this.AdornedElement);
            }
        }
    }

    public sealed class DropDownMenuButton : ToggleButton
    {
        public static readonly DependencyProperty DropDownContextMenuProperty = DependencyProperty.Register("DropDownContextMenu", typeof(ContextMenu), typeof(DropDownMenuButton), new UIPropertyMetadata(null));

        private bool suppressFlag = false; // メニューが開いた状態で、ボタンが再度押されても、メニューを開かないよう抑制する。

        public DropDownMenuButton()
        {
            var binding = new Binding("DropDownContextMenu.IsOpen") { Source = this };
            this.SetBinding(DropDownMenuButton.IsCheckedProperty, binding);
            this.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (this.DropDownContextMenu?.IsVisible == true)
                {
                    this.suppressFlag = true;
                }
            };
        }

        public ContextMenu? DropDownContextMenu
        {
            get { return this.GetValue(DropDownContextMenuProperty) as ContextMenu; }
            set { this.SetValue(DropDownContextMenuProperty, value); }
        }

        protected override void OnClick()
        {
            if (this.DropDownContextMenu == null)
            {
                return;
            }

            if (this.suppressFlag)
            {
                this.suppressFlag = false;
                return;
            }

            this.DropDownContextMenu.PlacementTarget = this;
            this.DropDownContextMenu.Placement = PlacementMode.Bottom;
            this.DropDownContextMenu.IsOpen = true;
        }
    }
}
