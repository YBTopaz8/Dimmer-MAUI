package crc64cb71f025204412d4;


public class PView_BackgroundBrushDrawable
	extends android.graphics.drawable.LayerDrawable
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_draw:(Landroid/graphics/Canvas;)V:GetDraw_Landroid_graphics_Canvas_Handler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Android.Internal.PView+BackgroundBrushDrawable, DevExpress.Maui.Core", PView_BackgroundBrushDrawable.class, __md_methods);
	}

	public PView_BackgroundBrushDrawable (android.graphics.drawable.Drawable[] p0)
	{
		super (p0);
		if (getClass () == PView_BackgroundBrushDrawable.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView+BackgroundBrushDrawable, DevExpress.Maui.Core", "Android.Graphics.Drawables.Drawable[], Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void draw (android.graphics.Canvas p0)
	{
		n_draw (p0);
	}

	private native void n_draw (android.graphics.Canvas p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
