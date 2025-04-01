package crc64159f3caeb1269279;


public class MauiPopup
	extends android.app.Dialog
	implements
		mono.android.IGCUserPeer,
		android.content.DialogInterface.OnCancelListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_show:()V:GetShowHandler\n" +
			"n_onTouchEvent:(Landroid/view/MotionEvent;)Z:GetOnTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_onCancel:(Landroid/content/DialogInterface;)V:GetOnCancel_Landroid_content_DialogInterface_Handler:Android.Content.IDialogInterfaceOnCancelListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Core.Views.MauiPopup, CommunityToolkit.Maui.Core", MauiPopup.class, __md_methods);
	}

	public MauiPopup (android.content.Context p0, boolean p1, android.content.DialogInterface.OnCancelListener p2)
	{
		super (p0, p1, p2);
		if (getClass () == MauiPopup.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Core.Views.MauiPopup, CommunityToolkit.Maui.Core", "Android.Content.Context, Mono.Android:System.Boolean, System.Private.CoreLib:Android.Content.IDialogInterfaceOnCancelListener, Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public MauiPopup (android.content.Context p0, int p1)
	{
		super (p0, p1);
		if (getClass () == MauiPopup.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Core.Views.MauiPopup, CommunityToolkit.Maui.Core", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public MauiPopup (android.content.Context p0)
	{
		super (p0);
		if (getClass () == MauiPopup.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Core.Views.MauiPopup, CommunityToolkit.Maui.Core", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void show ()
	{
		n_show ();
	}

	private native void n_show ();

	public boolean onTouchEvent (android.view.MotionEvent p0)
	{
		return n_onTouchEvent (p0);
	}

	private native boolean n_onTouchEvent (android.view.MotionEvent p0);

	public void onCancel (android.content.DialogInterface p0)
	{
		n_onCancel (p0);
	}

	private native void n_onCancel (android.content.DialogInterface p0);

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
