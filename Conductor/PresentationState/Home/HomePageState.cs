// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Arc.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx.Messaging;

namespace Conductor.State;

public partial class HomePageState : ObservableObject
{
    private readonly Generator generator = new();
    private readonly IBasicPresentationService simpleWindowService;

    [ObservableProperty]
    private string resultTextValue = string.Empty;

    [ObservableProperty]
    private bool toggleCopyToClipboard;

    public HomePageState(IBasicPresentationService simpleWindowService)
    {
        this.simpleWindowService = simpleWindowService;
        this.ToggleCopyToClipboard = true;
    }

    public void StoreState()
    {
    }

    [RelayCommand]
    private void Generate(string param)
    {
        var id = (GenerateId)Enum.Parse(typeof(GenerateId), param!);
        this.ResultTextValue = this.generator.Generate(id);
        if (this.ToggleCopyToClipboard)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(this.ResultTextValue);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
            }
        }
    }
}
