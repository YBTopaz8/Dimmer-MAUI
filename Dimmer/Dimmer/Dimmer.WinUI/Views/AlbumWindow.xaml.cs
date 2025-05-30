using Syncfusion.Maui.Toolkit.Carousel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;

public partial class AlbumWindow : Window
{
    public AlbumWindow(BaseViewModel vm, IMapper mapper)
    {
        InitializeComponent();
        this.Height = 600;
        this.Width = 800;
        BindingContext = vm;
    }

}