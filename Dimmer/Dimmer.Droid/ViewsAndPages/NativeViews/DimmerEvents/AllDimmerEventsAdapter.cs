using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidX.Lifecycle;
using DynamicData;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerEvents;

public partial class AllDimmerEventsAdapter : RecyclerView.Adapter
{
    public override int ItemCount => throw new NotImplementedException();

    public BaseViewModelAnd MyViewModel { get; }

    public AllDimmerEventsAdapter(BaseViewModelAnd vm)
    {
        MyViewModel = vm;

        var realm = MyViewModel.RealmFactory.GetRealmInstance();

        //var albumInDB = realm.All<DimmerPlayEvent>().AsObservableChangeSet()
        //    .transform;
        

    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        throw new NotImplementedException();
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        throw new NotImplementedException();
    }
}
