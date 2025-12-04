using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews;

public class PlayQueueFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    public PlayQueueFragment(BaseViewModelAnd vm) { MyViewModel = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        var header = new MaterialTextView(ctx) { Text = "Up Next", TextSize = 22 };
        header.SetPadding(30, 30, 30, 30);
        root.AddView(header);

        var recycler = new RecyclerView(ctx);
        recycler.SetLayoutManager(new LinearLayoutManager(ctx));

        // Mock data, replace with MyViewModel.Queue
        var queue = new List<string> { "Song A", "Song B", "Song C", "Song D", "Song E" };
        recycler.SetAdapter(new SimpleStringAdapter(queue));

        root.AddView(recycler);
        return root;
    }

    class SimpleStringAdapter : RecyclerView.Adapter
    {
        List<string> data;
        public SimpleStringAdapter(List<string> d) { data = d; }
        public override int ItemCount => data.Count;
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as VH).Text.Text = $"{position + 1}. {data[position]}";
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new TextView(parent.Context) { TextSize = 18 };
            tv.SetPadding(30, 20, 30, 20);
            return new VH(tv);
        }
        class VH : RecyclerView.ViewHolder { public TextView Text; public VH(View v) : base(v) { Text = (TextView)v; } }
    }
}