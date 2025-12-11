using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongPlayHistoryFragment : Fragment
{
    private BaseViewModelAnd _vm;
    public SongPlayHistoryFragment(BaseViewModelAnd vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var recycler = new RecyclerView(ctx);
        recycler.SetLayoutManager(new LinearLayoutManager(ctx));

        var events = _vm.SelectedSong?.PlayEvents ?? new System.Collections.ObjectModel.ObservableCollection<DimmerPlayEventView>();
        recycler.SetAdapter(new PlayHistoryAdapter(events.OrderByDescending(x => x.DatePlayed).ToList()));

        return recycler;
    }

    class PlayHistoryAdapter : RecyclerView.Adapter
    {
        List<DimmerPlayEventView> _items;
        public PlayHistoryAdapter(List<DimmerPlayEventView> items) { _items = items; }
        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var card = new MaterialCardView(parent.Context) { Radius = 8, Elevation = 2 };
            var ly = new LinearLayout(parent.Context) { Orientation = Orientation.Vertical };
            ; ly.SetPadding(16, 16, 16, 16);
            var type = new TextView(parent.Context) { Typeface = Typeface.DefaultBold };
            var date = new TextView(parent.Context);
            var device = new TextView(parent.Context) { TextSize = 12, Alpha = 0.7f };

            ly.AddView(type); ly.AddView(date); ly.AddView(device);
            card.AddView(ly);
            card.LayoutParameters = new ViewGroup.MarginLayoutParams(-1, -2) { BottomMargin = 12 };
            return new VH(card, type, date, device);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as VH;
            var item = _items[position];
            vh.Type.Text = item.PlayTypeStr ?? "Unknown Action";
            vh.Date.Text = item.DatePlayed.ToString("g"); // General date/time
            vh.Device.Text = $"{item.DeviceName} ({item.DeviceModel})";

            // Color code based on PlayType (Completed vs Skipped)
            vh.Type.SetTextColor(item.WasPlayCompleted ? Color.Green : Color.Red);
        }

        class VH : RecyclerView.ViewHolder
        {
            public TextView Type, Date, Device;
            public VH(View v, TextView t, TextView d, TextView dev) : base(v) { Type = t; Date = d; Device = dev; }
        }
    }
}