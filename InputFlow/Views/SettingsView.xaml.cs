using Microsoft.UI.Xaml.Controls;
using InputFlow.Core.ViewModels;

namespace InputFlow.Views;

/// <summary>Settings page.</summary>
public sealed partial class SettingsView : Page
{
    private readonly SettingsViewModel _vm;
    public SettingsView(SettingsViewModel vm) { _vm = vm; InitializeComponent(); }
}
