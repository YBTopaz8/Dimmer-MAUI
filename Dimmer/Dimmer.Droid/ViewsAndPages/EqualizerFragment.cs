using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages;

public class EqualizerFragment : Fragment
{
    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(30, 30, 30, 30);

        var title = new TextView(ctx) { Text = "Equalizer", TextSize = 24, Gravity = GravityFlags.Center };
        root.AddView(title);

        string[] bands = { "60Hz", "230Hz", "910Hz", "4kHz", "14kHz" };

        foreach (var band in bands)
        {
            var bandLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
            bandLayout.SetPadding(0, 20, 0, 20);

            var label = new TextView(ctx) { Text = band };
            var slider = new SeekBar(ctx) { Max = 100, Progress = 50 }; // 50 is 0db

            slider.ProgressChanged += (s, e) =>
            {
                // Hook to Audio FX
            };

            bandLayout.AddView(label);
            bandLayout.AddView(slider);
            root.AddView(bandLayout);
        }

        // Presets
        var presetScroll = new HorizontalScrollView(ctx);
        var presetContainer = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };

        string[] presets = { "Flat", "Bass Boost", "Rock", "Jazz", "Pop", "Classical" };
        foreach (var p in presets)
        {
            var chip = new Google.Android.Material.Chip.Chip(ctx) { Text = p, Checkable = true };
            presetContainer.AddView(chip);
        }

        presetScroll.AddView(presetContainer);
        root.AddView(presetScroll);

        return root;
    }
}