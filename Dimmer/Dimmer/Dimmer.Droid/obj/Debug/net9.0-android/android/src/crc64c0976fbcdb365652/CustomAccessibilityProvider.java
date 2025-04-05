package crc64c0976fbcdb365652;


public class CustomAccessibilityProvider
	extends androidx.core.view.accessibility.AccessibilityNodeProviderCompat
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_createAccessibilityNodeInfo:(I)Landroidx/core/view/accessibility/AccessibilityNodeInfoCompat;:GetCreateAccessibilityNodeInfo_IHandler\n" +
			"n_performAction:(IILandroid/os/Bundle;)Z:GetPerformAction_IILandroid_os_Bundle_Handler\n" +
			"";
		mono.android.Runtime.register ("Syncfusion.Maui.Toolkit.Graphics.Internals.CustomAccessibilityProvider, Syncfusion.Maui.Toolkit", CustomAccessibilityProvider.class, __md_methods);
	}

	public CustomAccessibilityProvider ()
	{
		super ();
		if (getClass () == CustomAccessibilityProvider.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Graphics.Internals.CustomAccessibilityProvider, Syncfusion.Maui.Toolkit", "", this, new java.lang.Object[] {  });
		}
	}

	public CustomAccessibilityProvider (java.lang.Object p0)
	{
		super (p0);
		if (getClass () == CustomAccessibilityProvider.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Graphics.Internals.CustomAccessibilityProvider, Syncfusion.Maui.Toolkit", "Java.Lang.Object, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public androidx.core.view.accessibility.AccessibilityNodeInfoCompat createAccessibilityNodeInfo (int p0)
	{
		return n_createAccessibilityNodeInfo (p0);
	}

	private native androidx.core.view.accessibility.AccessibilityNodeInfoCompat n_createAccessibilityNodeInfo (int p0);

	public boolean performAction (int p0, int p1, android.os.Bundle p2)
	{
		return n_performAction (p0, p1, p2);
	}

	private native boolean n_performAction (int p0, int p1, android.os.Bundle p2);

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
