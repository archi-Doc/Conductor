// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.WinUI;
using Microsoft.UI;

namespace Conductor;

[TinyhandObject]
public partial class AppOptions
{ // Application Options
    public const string Filename = "AppOptions.tinyhand";

    public AppOptions()
    {
    }

    [Key(0)]
    public BrushOption BrushTest { get; set; } = new(Colors.Red);

    [Key(1)]
    public BrushCollection BrushCollection { get; set; } = default!; // Brush Collection
}

[TinyhandObject]
public partial class BrushCollection : ITinyhandSerializationCallback
{
    [Key(0)]
    public BrushOption Brush1 { get; set; } = new(Colors.BurlyWood);

    public BrushOption this[string name]
    {
        get
        {
            return this.Brush1;
        }
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
    }

    public void OnAfterReconstruct()
    {
    }
}
