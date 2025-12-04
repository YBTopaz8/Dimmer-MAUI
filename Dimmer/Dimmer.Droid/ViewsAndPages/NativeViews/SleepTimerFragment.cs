using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Android.Material.Button;

namespace Dimmer.ViewsAndPages.NativeViews;


public class SleepTimerFragment : Fragment
{
    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
        };
        root.SetGravity(GravityFlags.Center);
        root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        var title = new MaterialTextView(ctx) { Text = "Sleep Timer", TextSize = 24 };
        title.Gravity = GravityFlags.Center;

        var timerStatus = new TextView(ctx) { Text = "Timer is off", TextSize = 16 };
        timerStatus.Gravity = GravityFlags.Center;
        timerStatus.SetPadding(0, 0, 0, 50);

        var grid = new GridLayout(ctx) { ColumnCount = 2, RowCount = 3 };

        int[] minutes = { 15, 30, 45, 60, 90, 120 };
        foreach (var min in minutes)
        {
            var btn = new MaterialButton(ctx) { Text = $"{min} min" };
            var p = new GridLayout.LayoutParams();
            p.SetMargins(10, 10, 10, 10);
            p.Width = 300; // Fixed width for uniformity
            btn.LayoutParameters = p;

            btn.Click += (s, e) =>
            {
                // Logic to set timer in ViewModel/Service
                timerStatus.Text = $"Stopping audio in {min} minutes";
                Toast.MakeText(ctx, $"Timer set for {min}m", ToastLength.Short).Show();
            };
            grid.AddView(btn);
        }

        var cancelBtn = new MaterialButton(ctx) { Text = "Cancel Timer" };
        cancelBtn.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.DarkRed);
        cancelBtn.Click += (s, e) => timerStatus.Text = "Timer is off";

        root.AddView(title);
        root.AddView(timerStatus);
        root.AddView(grid);
        root.AddView(cancelBtn);

        return root;
    }
}