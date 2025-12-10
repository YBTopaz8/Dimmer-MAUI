using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.ViewUtils;

public class SimpleStringAdapter : RecyclerView.Adapter
{
    private readonly ObservableCollection<string> _items;
    private readonly Action<string> _onItemClick;

    public SimpleStringAdapter(ObservableCollection<string> items, Action<string> onItemClick)
    {
        _items = items;
        _onItemClick = onItemClick;
        // Listen to collection changes to update UI automatically
        _items.CollectionChanged += (s, e) => NotifyDataSetChanged();
    }

    public override int ItemCount => _items.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is StringViewHolder vh)
        {
            vh.TextView.Text = _items[position];
            vh.ItemView.Click += (s, e) => _onItemClick(_items[position]);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var tv = new TextView(parent.Context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            TextSize = 16,
        };
        tv.SetPadding(40, 30, 40, 30);
        return new StringViewHolder(tv);
    }

    class StringViewHolder : RecyclerView.ViewHolder
    {
        public TextView TextView { get; }
        public StringViewHolder(TextView itemView) : base(itemView) => TextView = itemView;
    }
}