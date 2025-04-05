package crc64c0976fbcdb365652;


public class CustomAccessibilityDelegate
	extends androidx.core.view.AccessibilityDelegateCompat
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getAccessibilityNodeProvider:(Landroid/view/View;)Landroidx/core/view/accessibility/AccessibilityNodeProviderCompat;:GetGetAccessibilityNodeProvider_Landroid_view_View_Handler\n" +
			"n_dispatchPopulateAccessibilityEvent:(Landroid/view/View;Landroid/view/accessibility/AccessibilityEvent;)Z:GetDispatchPopulateAccessibilityEvent_Landroid_view_View_Landroid_view_accessibility_AccessibilityEvent_Handler\n" +
			"";
		mono.android.Runtime.register ("Syncfusion.Maui.Toolkit.Graphics.Internals.CustomAccessibilityDelegate, Syncfusion.Maui.Toolkit", CustomAccessibilityDelegate.class, __md_methods);
	}

	public CustomAccessibilityDelegate ()
	{
		super ();
		if (getClass () == CustomAccessibilityDelegate.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Graphics.Internals.CustomAccessibilityDelegate, Syncfusion.Maui.Toolkit", "", this, new java.lang.Object[] {  });
		}
	}

	public CustomAccessibilityDelegate (android.view.View.AccessibilityDelegate p0)
	{
		super (p0);
		if (getClass () == CustomAccessibilityDelegate.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Graphics.Internals.CustomAccessibilityDelegate, Syncfusion.Maui.Toolkit", "Android.Views.View+AccessibilityDelegate, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public androidx.core.view.accessibility.AccessibilityNodeProviderCompat getAccessibilityNodeProvider (android.view.View p0)
	{
		return n_getAccessibilityNodeProvider (p0);
	}

	private native androidx.core.view.accessibility.AccessibilityNodeProviderCompat n_getAccessibilityNodeProvider (android.view.View p0);

	public boolean dispatchPopulateAccessibilityEvent (android.view.View p0, android.view.accessibility.AccessibilityEvent p1)
	{
		return n_dispatchPopulateAccessibilityEvent (p0, p1);
	}

	private native boolean n_dispatchPopulateAccessibilityEvent (android.view.View p0, android.view.accessibility.AccessibilityEvent p1);

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
