using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bumptech.Glide;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.TextView;

using static Android.InputMethodServices.Keyboard;
using static Android.Provider.MediaStore.Audio;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong
{
    internal class SongNotesRecyclerViewAdapter:RecyclerView.Adapter
    {
        private BaseViewModelAnd myViewModel;

        public SongNotesRecyclerViewAdapter(BaseViewModelAnd myViewModel)
        {
            this.myViewModel = myViewModel;
        }

        public override int ItemCount => myViewModel.SelectedSong.UserNoteAggregatedCol.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
         
            if (holder is SongNoteViewHolder songNoteHolder)
            {
                UserNoteModelView? note = myViewModel.SelectedSong.UserNoteAggregatedCol[position];
                songNoteHolder.Bind(note);

            }
        }


        LinearLayout row;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var ctx = parent.Context;
            var materialCardView = new MaterialCardView(parent.Context)
            {

                CardElevation = AppUtil.DpToPx(4),
                Radius = AppUtil.DpToPx(8),
            };
            var lyParams = new RecyclerView.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
                );
            lyParams.SetMargins(AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5));

            // set color gray if light theme and dark gray if dark theme
            var isDarkTheme = ctx.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes);

            materialCardView.LayoutParameters = lyParams;
            materialCardView.StrokeWidth = 2;
            materialCardView.StrokeColor = isDarkTheme ? Color.MidnightBlue : Color.Gray;

           var GridOfTwoColumbns = new GridLayout(ctx)
            {
                ColumnCount = 2,
                RowCount = 0,
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                    ),
            };

            var textEditLayoutEightyPercent = new MaterialTextView(ctx)
            {
                LayoutParameters = new ViewGroup.LayoutParams(
                
                ViewGroup.LayoutParams.MatchParent,
                 ViewGroup.LayoutParams.MatchParent
                )
            };
            GridOfTwoColumbns.AddView(textEditLayoutEightyPercent);

            var linearLayout = new LinearLayout(ctx)
            {
                LayoutParameters = new ViewGroup.LayoutParams
                (
                    AppUtil.DpToPx(150),
                    ViewGroup.LayoutParams.MatchParent
                    )
            };
            var editBtn = new MaterialButton(ctx)
            {
                LayoutParameters = new ViewGroup.LayoutParams
                (AppUtil.DpToPx(70), ViewGroup.LayoutParams.MatchParent)
            };
            Glide.With(ctx).Load(Resource.Drawable.edit)
                .Into(editBtn);
            var deleteBtn = new MaterialButton(ctx)
            {
                LayoutParameters = new ViewGroup.LayoutParams
                (AppUtil.DpToPx(70), ViewGroup.LayoutParams.MatchParent)
            };
            Glide.With(ctx).Load(Resource.Drawable.delete)
                .Into(deleteBtn);
            linearLayout.AddView(editBtn);
            linearLayout.AddView(deleteBtn);

            GridOfTwoColumbns.AddView(linearLayout);

            return new SongNoteViewHolder(materialCardView, myViewModel, editBtn, deleteBtn);

        }
    }

    internal partial class SongNoteViewHolder : RecyclerView.ViewHolder
    {
        public BaseViewModelAnd MyViewModel { get; }
        public MaterialButton EditBtn { get; }
        public MaterialButton DeleteImgBtn { get; }
        UserNoteModelView CurrentNote { get; set; }

        public SongNoteViewHolder( View itemView, BaseViewModelAnd myViewModel, MaterialButton editBtn
            ,MaterialButton deletbtn) : base(itemView)
        {
            MyViewModel = myViewModel;
            EditBtn = editBtn;
            DeleteImgBtn = deletbtn;

            EditBtn.Click += EditBtn_Click;
            DeleteImgBtn.Click += DeleteImgBtn_Click;
        }

        private void DeleteImgBtn_Click(object? sender, EventArgs e)
        {
            Toast.MakeText(DeleteImgBtn.Context, "Delete Note Clicked", ToastLength.Short)?.Show();
        }

        private void EditBtn_Click(object? sender, EventArgs e)
        {

            Toast.MakeText(EditBtn.Context, "Edit Note Clicked", ToastLength.Short)?.Show();

        }

        internal void Bind(UserNoteModelView note)
        {
            CurrentNote = note;
        }
    }
}
