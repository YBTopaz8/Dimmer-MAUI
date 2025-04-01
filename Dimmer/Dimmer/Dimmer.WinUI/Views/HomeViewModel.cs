

global using AutoMapper;
using CommunityToolkit.Mvvm.Input;
using Dimmer.ViewModel;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModel
{
    public readonly BaseViewModel _base;
    private readonly IMapper _mapper;
    public HomeViewModel(BaseViewModel baseVm, IMapper mapper) : base(baseVm.BaseAppFlow, mapper)
    {
        _base = baseVm;
        _mapper= mapper;

    }

    [RelayCommand]
    public void PlaySong()
    {

    }

}
