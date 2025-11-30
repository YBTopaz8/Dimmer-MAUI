

namespace Dimmer.ViewsAndPages.NativeViews
{
    internal class NowPlayingQueueCarouselAdapter: RecyclerView.Adapter
    {
        private BaseViewModelAnd myViewModel;

        public NowPlayingQueueCarouselAdapter(BaseViewModelAnd myViewModel)
        {
            this.myViewModel = myViewModel;
        }

        public override int ItemCount => throw new NotImplementedException();

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            throw new NotImplementedException();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            throw new NotImplementedException();
        }
    }
}