using Bumptech.Glide;
using Google.Android.Material.Chip;

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

            // Main container - vertical layout
            var mainLayout = new LinearLayout(ctx)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };
            mainLayout.SetPadding(AppUtil.DpToPx(12), AppUtil.DpToPx(8), AppUtil.DpToPx(12), AppUtil.DpToPx(8));

            // Header row - timestamp, author, and public/private indicator
            var headerRow = new LinearLayout(ctx)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };

            // Timestamp badge (clickable)
            var timestampChip = new Chip(ctx)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };
            timestampChip.SetChipIconResource(Resource.Drawable.exo_ic_skip_previous);
            timestampChip.Clickable = true;
            timestampChip.Focusable = true;
            headerRow.AddView(timestampChip);

            // Author name
            var authorText = new MaterialTextView(ctx)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WrapContent,
                    1.0f
                )
            };
            authorText.SetTextSize(Android.Util.ComplexUnitType.Sp, 12);
            authorText.Gravity = GravityFlags.CenterVertical;
            headerRow.AddView(authorText);

            // Public/Private indicator chip
            var visibilityChip = new Chip(ctx)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };
            headerRow.AddView(visibilityChip);

            mainLayout.AddView(headerRow);

            // Note text
            var noteText = new MaterialTextView(ctx)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };
            noteText.SetTextSize(Android.Util.ComplexUnitType.Sp, 14);
            noteText.SetPadding(0, AppUtil.DpToPx(8), 0, AppUtil.DpToPx(8));
            mainLayout.AddView(noteText);

            // Reactions row
            var reactionsRow = new LinearLayout(ctx)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };

            // Reaction buttons
            var likeBtn = CreateReactionButton(ctx, "👍", "like");
            var fireBtn = CreateReactionButton(ctx, "🔥", "fire");
            var heartBtn = CreateReactionButton(ctx, "❤️", "heart");
            var sadBtn = CreateReactionButton(ctx, "😢", "sad");

            reactionsRow.AddView(likeBtn);
            reactionsRow.AddView(fireBtn);
            reactionsRow.AddView(heartBtn);
            reactionsRow.AddView(sadBtn);

            mainLayout.AddView(reactionsRow);

            // Action buttons row
            var actionsRow = new LinearLayout(ctx)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                ),
                Gravity = GravityFlags.End
            };

            var editBtn = new MaterialButton(ctx)
            {
                Text = "Edit",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };

            var deleteBtn = new MaterialButton(ctx)
            {
                Text = "Delete",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };

            actionsRow.AddView(editBtn);
            actionsRow.AddView(deleteBtn);
            mainLayout.AddView(actionsRow);

            materialCardView.AddView(mainLayout);

            return new SongNoteViewHolder(
                materialCardView, 
                myViewModel, 
                noteText,
                timestampChip,
                authorText,
                visibilityChip,
                likeBtn,
                fireBtn,
                heartBtn,
                sadBtn,
                editBtn, 
                deleteBtn
            );
        }

        private MaterialButton CreateReactionButton(Context ctx, string emoji, string reactionType)
        {
            var btn = new MaterialButton(ctx)
            {
                Text = emoji,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };
            btn.SetTag(Resource.String.app_name, reactionType); // Store reaction type
            return btn;
        }
    }

    internal partial class SongNoteViewHolder : RecyclerView.ViewHolder
    {
        public BaseViewModelAnd MyViewModel { get; }
        public MaterialTextView NoteText { get; }
        public Chip TimestampChip { get; }
        public MaterialTextView AuthorText { get; }
        public Chip VisibilityChip { get; }
        public MaterialButton LikeBtn { get; }
        public MaterialButton FireBtn { get; }
        public MaterialButton HeartBtn { get; }
        public MaterialButton SadBtn { get; }
        public MaterialButton EditBtn { get; }
        public MaterialButton DeleteBtn { get; }
        UserNoteModelView CurrentNote { get; set; }

        public SongNoteViewHolder(
            View itemView, 
            BaseViewModelAnd myViewModel,
            MaterialTextView noteText,
            Chip timestampChip,
            MaterialTextView authorText,
            Chip visibilityChip,
            MaterialButton likeBtn,
            MaterialButton fireBtn,
            MaterialButton heartBtn,
            MaterialButton sadBtn,
            MaterialButton editBtn,
            MaterialButton deleteBtn) : base(itemView)
        {
            MyViewModel = myViewModel;
            NoteText = noteText;
            TimestampChip = timestampChip;
            AuthorText = authorText;
            VisibilityChip = visibilityChip;
            LikeBtn = likeBtn;
            FireBtn = fireBtn;
            HeartBtn = heartBtn;
            SadBtn = sadBtn;
            EditBtn = editBtn;
            DeleteBtn = deleteBtn;

            // Wire up events
            TimestampChip.Click += TimestampChip_Click;
            LikeBtn.Click += (s, e) => ReactionBtn_Click("like");
            FireBtn.Click += (s, e) => ReactionBtn_Click("fire");
            HeartBtn.Click += (s, e) => ReactionBtn_Click("heart");
            SadBtn.Click += (s, e) => ReactionBtn_Click("sad");
            EditBtn.Click += EditBtn_Click;
            DeleteBtn.Click += DeleteBtn_Click;
        }

        private void TimestampChip_Click(object? sender, EventArgs e)
        {
            if (CurrentNote?.TimestampMs != null)
            {
                // Seek to timestamp
                var positionInSeconds = CurrentNote.TimestampMs.Value / 1000.0;
                MyViewModel.SeekTrackPosition(positionInSeconds);
                Toast.MakeText(TimestampChip.Context, $"Seeking to {CurrentNote.TimestampDisplay}", ToastLength.Short)?.Show();
            }
        }

        private void ReactionBtn_Click(string reactionType)
        {
            if (CurrentNote == null) return;

            // Toggle reaction
            if (CurrentNote.Reactions == null)
                CurrentNote.Reactions = new Dictionary<string, int>();

            if (CurrentNote.Reactions.ContainsKey(reactionType))
                CurrentNote.Reactions[reactionType]++;
            else
                CurrentNote.Reactions[reactionType] = 1;

            // Update button text to show count
            UpdateReactionButtons();
            
            Toast.MakeText(LikeBtn.Context, $"Reacted with {reactionType}", ToastLength.Short)?.Show();
        }

        private void DeleteBtn_Click(object? sender, EventArgs e)
        {
            if (CurrentNote == null) return;
            
            // TODO: Call ViewModel to delete note
            Toast.MakeText(DeleteBtn.Context, "Delete Note Clicked", ToastLength.Short)?.Show();
        }

        private void EditBtn_Click(object? sender, EventArgs e)
        {
            if (CurrentNote == null) return;
            
            // Show edit dialog
            var dialog = new SongCommentDialogFragment(MyViewModel, CurrentNote);
            // Note: We need FragmentManager from the context, this would be better handled at a higher level
            Toast.MakeText(EditBtn.Context, "Edit functionality - use dialog from fragment", ToastLength.Short)?.Show();
        }

        internal void Bind(UserNoteModelView note)
        {
            CurrentNote = note;

            // Bind text
            NoteText.Text = note.UserMessageText ?? string.Empty;

            // Bind timestamp
            if (note.TimestampMs != null)
            {
                TimestampChip.Visibility = ViewStates.Visible;
                TimestampChip.Text = note.TimestampDisplay ?? "00:00";
            }
            else
            {
                TimestampChip.Visibility = ViewStates.Gone;
            }

            // Bind author
            if (!string.IsNullOrEmpty(note.AuthorUsername))
            {
                AuthorText.Text = $"@{note.AuthorUsername}";
                AuthorText.Visibility = ViewStates.Visible;
            }
            else
            {
                AuthorText.Visibility = ViewStates.Gone;
            }

            // Bind visibility
            VisibilityChip.Text = note.IsPublic ? "Public" : "Private";
            // Use more appropriate icons for visibility
            // Note: Using basic icons - ideally would use visibility/visibility_off icons
            VisibilityChip.SetChipIconResource(note.IsPublic ? 
                Android.Resource.Drawable.IcMenuView : 
                Android.Resource.Drawable.IcSecureIndicator);

            // Update reactions
            UpdateReactionButtons();

            // Show/hide action buttons based on ownership
            // For now, show all buttons. Later can check if current user is author
            EditBtn.Visibility = ViewStates.Visible;
            DeleteBtn.Visibility = ViewStates.Visible;
        }

        private void UpdateReactionButtons()
        {
            if (CurrentNote?.Reactions == null)
            {
                LikeBtn.Text = "👍";
                FireBtn.Text = "🔥";
                HeartBtn.Text = "❤️";
                SadBtn.Text = "😢";
                return;
            }

            LikeBtn.Text = $"👍 {(CurrentNote.Reactions.ContainsKey("like") ? CurrentNote.Reactions["like"] : 0)}";
            FireBtn.Text = $"🔥 {(CurrentNote.Reactions.ContainsKey("fire") ? CurrentNote.Reactions["fire"] : 0)}";
            HeartBtn.Text = $"❤️ {(CurrentNote.Reactions.ContainsKey("heart") ? CurrentNote.Reactions["heart"] : 0)}";
            SadBtn.Text = $"😢 {(CurrentNote.Reactions.ContainsKey("sad") ? CurrentNote.Reactions["sad"] : 0)}";
        }
    }
}
