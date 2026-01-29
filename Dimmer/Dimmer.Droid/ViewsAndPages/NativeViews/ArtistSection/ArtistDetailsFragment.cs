using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.ArtistSection;

public partial class ArtistDetailsFragment : Fragment
{

    public ArtistDetailsFragment()
    {
        
    }

    public ArtistDetailsFragment(BaseViewModelAnd viewModel)
    {

        ViewModel = viewModel;
    }

    public BaseViewModelAnd ViewModel { get; }

    public override void OnResume()
    {
        base.OnResume();
    }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return base.OnCreateView(inflater, container, savedInstanceState);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
    }
}
