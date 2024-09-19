// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.WinUI;

namespace Conductor;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class AppSettings : ITinyhandSerializationCallback
{// Application Settings
    public const string Filename = "AppSettings.tinyhand";

    public DipWindowPlacement WindowPlacement { get; set; } = default!;

    public string Culture { get; set; } = string.Empty;

    public double ViewScale { get; set; } = 1.0d;

    public bool TogglePreventShutdownWhileBusy { get; set; } = false;

    public void OnAfterDeserialize()
    {
        Scaler.ViewScale = this.ViewScale;
    }

    public void OnBeforeSerialize()
    {
        this.ViewScale = Scaler.ViewScale;
    }

    public void OnAfterReconstruct()
    {
    }
}
