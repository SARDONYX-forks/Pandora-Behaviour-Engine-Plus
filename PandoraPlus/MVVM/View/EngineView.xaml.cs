using System.Windows;
using System.Windows.Controls;
using Pandora.MVVM.ViewModel;

namespace Pandora.MVVM.View;

/// <summary>
/// Interaction logic for EngineView.xaml
/// </summary>
public partial class EngineView : UserControl
{
    private readonly EngineViewModel _viewModel;

    public EngineView()
    {
        InitializeComponent();

        this._viewModel = new EngineViewModel();
        this.DataContext = this._viewModel;
        Loaded += this.EngineViewLoaded;
    }
    private async void EngineViewLoaded(object sender, RoutedEventArgs e)
    {
        await this._viewModel.LoadAsync();
    }
}
