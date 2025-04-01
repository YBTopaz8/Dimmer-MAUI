package crc648fc34c62be8fbbff;


public class Snackbar_SnackbarCallback
	extends com.google.android.material.snackbar.BaseTransientBottomBar.BaseCallback
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onShown:(Ljava/lang/Object;)V:GetOnShown_Ljava_lang_Object_Handler\n" +
			"n_onDismissed:(Ljava/lang/Object;I)V:GetOnDismissed_Ljava_lang_Object_IHandler\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Alerts.Snackbar+SnackbarCallback, CommunityToolkit.Maui", Snackbar_SnackbarCallback.class, __md_methods);
	}

	public Snackbar_SnackbarCallback ()
	{
		super ();
		if (getClass () == Snackbar_SnackbarCallback.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Alerts.Snackbar+SnackbarCallback, CommunityToolkit.Maui", "", this, new java.lang.Object[] {  });
		}
	}

	public void onShown (java.lang.Object p0)
	{
		n_onShown (p0);
	}

	private native void n_onShown (java.lang.Object p0);

	public void onDismissed (java.lang.Object p0, int p1)
	{
		n_onDismissed (p0, p1);
	}

	private native void n_onDismissed (java.lang.Object p0, int p1);

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
