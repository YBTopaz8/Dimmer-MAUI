package crc640fd0ddb16fe433d4;


public class TouchBehavior_AccessibilityListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.view.accessibility.AccessibilityManager.AccessibilityStateChangeListener,
		android.view.accessibility.AccessibilityManager.TouchExplorationStateChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onAccessibilityStateChanged:(Z)V:GetOnAccessibilityStateChanged_ZHandler:Android.Views.Accessibility.AccessibilityManager/IAccessibilityStateChangeListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onTouchExplorationStateChanged:(Z)V:GetOnTouchExplorationStateChanged_ZHandler:Android.Views.Accessibility.AccessibilityManager/ITouchExplorationStateChangeListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Behaviors.TouchBehavior+AccessibilityListener, CommunityToolkit.Maui", TouchBehavior_AccessibilityListener.class, __md_methods);
	}

	public TouchBehavior_AccessibilityListener ()
	{
		super ();
		if (getClass () == TouchBehavior_AccessibilityListener.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Behaviors.TouchBehavior+AccessibilityListener, CommunityToolkit.Maui", "", this, new java.lang.Object[] {  });
		}
	}

	public void onAccessibilityStateChanged (boolean p0)
	{
		n_onAccessibilityStateChanged (p0);
	}

	private native void n_onAccessibilityStateChanged (boolean p0);

	public void onTouchExplorationStateChanged (boolean p0)
	{
		n_onTouchExplorationStateChanged (p0);
	}

	private native void n_onTouchExplorationStateChanged (boolean p0);

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
