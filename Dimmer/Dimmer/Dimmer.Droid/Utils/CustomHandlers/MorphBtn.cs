using AndMorphingButton;

using Microsoft.Maui.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TheRealNativeButton = AndMorphingButton.MorphingButton;
namespace Dimmer.Utils.CustomHandlers;
// This handler connects your cross-platform MorphingButtonView
// to the real native Android.MorphingButton.MorphingButton.
public class MorphingButtonHandler : ViewHandler<MorphingButtonView, TheRealNativeButton>
{
    // PropertyMapper allows you to update native properties when
    // MAUI bindable properties change. We can leave it empty for now.
    public static IPropertyMapper<MorphingButtonView, MorphingButtonHandler> PropertyMapper =
        new PropertyMapper<MorphingButtonView, MorphingButtonHandler>() { };

    public MorphingButtonHandler() : base(PropertyMapper)
    {
    }

    // This is the most important method. It creates the actual native control.
    protected override TheRealNativeButton CreatePlatformView()
    {
        // We use the MauiContext to get the native Android Context
        return new TheRealNativeButton(Context);
    }

    // Use this to hook up events
    protected override void ConnectHandler(TheRealNativeButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Click += OnPlatformViewClicked;
    }

    // Use this to clean up events to prevent memory leaks
    protected override void DisconnectHandler(TheRealNativeButton platformView)
    {
        platformView.Click -= OnPlatformViewClicked;
        base.DisconnectHandler(platformView);
    }
    int count = 0;
    // Event handler for the native click
    private void OnPlatformViewClicked(object? sender, System.EventArgs e)
    {

        this.PlatformView.Text = "Clicked! YB" + count++;
        
        System.Diagnostics.Debug.WriteLine("Native Button was clicked via the HANDLER!");

        MorphingButton.Params circle = MorphingButton.Params.Create();
        circle.Duration(500);
        circle.CornerRadius(10); // 56 dp
        circle.Width(30); // 56 dp
        circle.Height(30); // 56 dp
        circle.Color(Android.Graphics.Color.Green); // normal state color
        circle.ColorPressed(Android.Graphics.Color.DarkSlateBlue); // pressed state color
        this.PlatformView.Morph(circle);

        //.duration(500)
        //.cornerRadius(dimen(R.dimen.mb_height_56)) // 56 dp
        //.width(dimen(R.dimen.mb_height_56)) // 56 dp
        //.height(dimen(R.dimen.mb_height_56)) // 56 dp
        //.color(color(R.color.green)) // normal state color
        //.colorPressed(color(R.color.green_dark)) // pressed state color
        //.icon(R.drawable.ic_done); // icon
        //btnMorph.morph(circle);
        // You can also invoke an event on the cross-platform View here if needed
    }
}