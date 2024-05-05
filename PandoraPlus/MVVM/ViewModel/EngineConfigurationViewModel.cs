using System.Collections.ObjectModel;
using System.ComponentModel;
using Pandora.Command;
using Pandora.Core;
using Pandora.Core.Engine.Configs;

namespace Pandora.MVVM.ViewModel;
public interface IEngineConfigurationViewModel : INotifyPropertyChanged
{
    public RelayCommand? SetCommand { get; }
    public ObservableCollection<IEngineConfigurationViewModel> NestedViewModels { get; }
}
public class EngineConfigurationViewModel<T> : IEngineConfigurationFactory, IEngineConfigurationViewModel where T : class, IEngineConfiguration, new()
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; private set; }
    public IEngineConfiguration? Config => new T();

    public ObservableCollection<IEngineConfigurationViewModel> NestedViewModels { get; private set; } = new ObservableCollection<IEngineConfigurationViewModel>();

    public RelayCommand? SetCommand { get; } = null;

    public EngineConfigurationViewModel(string name, RelayCommand setCommand)
    {
        this.Name = name;
        this.SetCommand = setCommand;
    }

}

public class EngineConfigurationViewModelContainer : IEngineConfigurationViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; private set; }

    public RelayCommand? SetCommand { get; } = null;

    public ObservableCollection<IEngineConfigurationViewModel> NestedViewModels { get; private set; } = new ObservableCollection<IEngineConfigurationViewModel>();
    public EngineConfigurationViewModelContainer(string name, params IEngineConfigurationViewModel[] viewModels)
    {
        this.Name = name;
        foreach (IEngineConfigurationViewModel viewModel in viewModels) { this.NestedViewModels.Add(viewModel); }
    }
}

