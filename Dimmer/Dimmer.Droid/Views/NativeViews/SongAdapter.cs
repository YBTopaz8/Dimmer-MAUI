
using Android.Content;

using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Transitions;

using DynamicData;

using Google.Android.Material.Transition;

using Java.Security;

using Microsoft.Maui.Controls;

using ReactiveUI;

using Color = Android.Graphics.Color;
using ImageButton = Android.Widget.ImageButton;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;
namespace Dimmer.Views.NativeViews
{
    internal class SongAdapter : RecyclerView.Adapter
    {
        private Context ctx;
        private BaseViewModelAnd vm;
        private IEnumerable<SongModelView> _songs = Enumerable.Empty<SongModelView>();
        private readonly IDisposable _subscription;

        public SongAdapter(Context ctx, BaseViewModelAnd myViewModel, IEnumerable<SongModelView> songs)
        {
            this.ctx = ctx;
            this.vm = myViewModel;
            _subscription = vm.searchResultsHolder
           .Connect()
           .ObserveOn(RxApp.MainThreadScheduler)
           .Subscribe(changes =>
           {
               _songs = vm.SearchResults;   // update enumerable reference
               NotifyDataSetChanged();
           });
        }

        public override int ItemCount => _songs.Count();

        public override void OnBindViewHolder(AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder holder, int position)
        {
            if (holder is SongViewHolder vh)
            {
                // find song in given position
                var song = _songs.ElementAt(position);
                vm.SelectedSong= song;
                vh.SongTitle.Text = song.Title;
                vh.AlbumName.Text = song.AlbumName ?? "Unknown";
                // handle image
                if (!string.IsNullOrEmpty(song.CoverImagePath) && System.IO.File.Exists(song.CoverImagePath))
                {
                    // Load from disk
                    var bmp = Android.Graphics.BitmapFactory.DecodeFile(song.CoverImagePath);
                    vh.ImageBtn.SetImageBitmap(bmp);
                }
               
                else
                {
                    // Fallback placeholder
                    vh.ImageBtn.SetImageResource(Resource.Drawable.musicnotess);
                }
                // ensure unique transition name
                var transitionName = $"sharedImage_{position}";
                ViewCompat.SetTransitionName(vh.ImageBtn, transitionName);

                vh.ImageBtn.Click += (s, e) =>
                {
                    OpenDetailFragment(vh.ImageBtn, transitionName);
                };
            }
        }

        private void OpenDetailFragment(View sharedView, string transitionName)
        {
            if (ctx is not AndroidX.Fragment.App.FragmentActivity activity) return;

            var fragment = new DetailFragment(transitionName);

            var hostFragment = activity.SupportFragmentManager.FindFragmentById(TransitionActivity.MyStaticID);
            if (hostFragment == null) return;

            // source fragment’s exit transform
            var exit = new MaterialElevationScale(true);
            exit.SetDuration(200);
            var reenter = new MaterialElevationScale(false);
            reenter.SetDuration(200);
            hostFragment.ExitTransition = exit;
            hostFragment.ReenterTransition = reenter;

            activity.SupportFragmentManager
                .BeginTransaction()
                .SetReorderingAllowed(true)
                .AddSharedElement(sharedView, transitionName)
                .Replace(TransitionActivity.MyStaticID, fragment)
                .AddToBackStack(null)
                .Commit();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _subscription.Dispose();
        }
        public override AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
            row.SetPadding(0, 20, 0, 20);
            row.SetGravity(GravityFlags.CenterVertical);

            var imgBtn = new ImageButton(ctx);
            imgBtn.LayoutParameters = new LinearLayout.LayoutParams(100, 100);
            imgBtn.SetBackgroundColor(Android.Graphics.Color.Transparent);
            row.AddView(imgBtn);

            var textCol = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
            var title = new TextView(ctx) { TextSize = 19 };
            var artist = new TextView(ctx) { TextSize = 15 };
            var album = new TextView(ctx) { TextSize = 11};
            
            album.SetTextColor(Color.Gray);
            textCol.AddView(title);
            textCol.AddView(artist);
            textCol.AddView(album);
            var lp2 = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
            row.AddView(textCol, lp2);

            var moreBtn = new ImageButton(ctx);
            moreBtn.SetBackgroundColor(Color.Transparent);
            moreBtn.SetImageResource(Resource.Drawable.more1);
            row.AddView(moreBtn, new LinearLayout.LayoutParams(90, 90));

            return new SongViewHolder(row, imgBtn, title, album, artist);
        }

        class SongViewHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
        {
            public ImageButton ImageBtn { get; }
            public TextView SongTitle { get; }
            public TextView AlbumName { get; }
            public TextView ArtistName { get; }

            public SongViewHolder(View itemView, ImageButton img, TextView title, TextView album, TextView artistName)
                : base(itemView)
            {
                ImageBtn = img;
                SongTitle = title;
                AlbumName = album;
                ArtistName = artistName;
            }
        }

    }
}