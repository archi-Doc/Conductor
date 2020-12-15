// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Arc.Mvvm;
using Tinyhand;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.WPF
{
    [TinyhandObject]
    public partial class BrushOption : BindableBase, ITinyhandSerializationCallback
    { // Constructor -> (OnAfterDeserialize()) -> Prepare() -> ... -> OnBeforeSerialize()
        private Color initialColor;
        private SolidColorBrush? brush;

        public BrushOption()
            : this(Colors.Black)
        {
        }

        public BrushOption(Color initialColor)
        {
            this.initialColor = initialColor;
            if (this.Brush == null)
            {
                this.Brush = new SolidColorBrush(initialColor);
            }
        }

        [IgnoreMember]
        public SolidColorBrush? Brush
        {
            get { return this.brush; }
            private set { this.SetProperty(ref this.brush, value); }
        }

        [Key(0)]
        public bool ChangedFlag { get; set; } // true:changed, false:default

        [Key(1)]
        public int BrushColor { get; set; }

        public void Change(Color color)
        {
            this.Brush = new SolidColorBrush(color);
            this.ChangedFlag = true;
        }

        public void OnAfterDeserialize()
        { // After data has loaded.
            if (this.ChangedFlag)
            {
                this.Brush = new SolidColorBrush(Color.FromArgb((byte)(this.BrushColor >> 24), (byte)(this.BrushColor >> 16), (byte)(this.BrushColor >> 8), (byte)this.BrushColor));
            }
        }

        public void OnBeforeSerialize()
        { // Before data is saved.
            if (this.Brush != null)
            {
                this.BrushColor = (this.Brush.Color.A << 24) | (this.Brush.Color.R << 16) | (this.Brush.Color.G << 8) | this.Brush.Color.B;
            }
            else
            {
                this.BrushColor = 0;
            }
        }
    }
}
