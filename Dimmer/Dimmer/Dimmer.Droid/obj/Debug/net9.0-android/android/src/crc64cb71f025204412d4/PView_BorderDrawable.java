package crc64cb71f025204412d4;


public class PView_BorderDrawable
	extends android.graphics.drawable.ColorDrawable
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_draw:(Landroid/graphics/Canvas;)V:GetDraw_Landroid_graphics_Canvas_Handler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Android.Internal.PView+BorderDrawable, DevExpress.Maui.Core", PView_BorderDrawable.class, __md_methods);
	}

	public PView_BorderDrawable ()
	{
		super ();
		if (getClass () == PView_BorderDrawable.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView+BorderDrawable, DevExpress.Maui.Core", "", this, new java.lang.Object[] {  });
		}
	}

	public PView_BorderDrawable (int p0)
	{
		super (p0);
		if (getClass () == PView_BorderDrawable.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView+BorderDrawable, DevExpress.Maui.Core", "Android.Graphics.Color, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public PView_BorderDrawable (crc64cb71f025204412d4.PView p0)
	{
		super ();
		if (getClass () == PView_BorderDrawable.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView+BorderDrawable, DevExpress.Maui.Core", "DevExpress.Maui.Core.Android.Internal.PView, DevExpress.Maui.Core", this, new java.lang.Object[] { p0 });
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
