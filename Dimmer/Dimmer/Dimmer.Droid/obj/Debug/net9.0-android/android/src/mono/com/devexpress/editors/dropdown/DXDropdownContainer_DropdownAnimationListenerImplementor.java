package mono.com.devexpress.editors.dropdown;


public class DXDropdownContainer_DropdownAnimationListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.dropdown.DXDropdownContainer.DropdownAnimationListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_closingAnimationComplete:()V:GetClosingAnimationCompleteHandler:DevExpress.Android.Editors.Dropdown.DXDropdownContainer/IDropdownAnimationListenerInvoker, DXEditors.a\n" +
			"n_closingAnimationWillStart:()V:GetClosingAnimationWillStartHandler:DevExpress.Android.Editors.Dropdown.DXDropdownContainer/IDropdownAnimationListenerInvoker, DXEditors.a\n" +
			"n_openingAnimationComplete:()V:GetOpeningAnimationCompleteHandler:DevExpress.Android.Editors.Dropdown.DXDropdownContainer/IDropdownAnimationListenerInvoker, DXEditors.a\n" +
			"n_openingAnimationWillStart:()V:GetOpeningAnimationWillStartHandler:DevExpress.Android.Editors.Dropdown.DXDropdownContainer/IDropdownAnimationListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.Dropdown.DXDropdownContainer+IDropdownAnimationListenerImplementor, DXEditors.a", DXDropdownContainer_DropdownAnimationListenerImplementor.class, __md_methods);
	}

	public DXDropdownContainer_DropdownAnimationListenerImplementor ()
	{
		super ();
		if (getClass () == DXDropdownContainer_DropdownAnimationListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.Dropdown.DXDropdownContainer+IDropdownAnimationListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void closingAnimationComplete ()
	{
		n_closingAnimationComplete ();
	}

	private native void n_closingAnimationComplete ();

	public void closingAnimationWillStart ()
	{
		n_closingAnimationWillStart ();
	}

	private native void n_closingAnimationWillStart ();

	public void openingAnimationComplete ()
	{
		n_openingAnimationComplete ();
	}

	private native void n_openingAnimationComplete ();

	public void openingAnimationWillStart ()
	{
		n_openingAnimationWillStart ();
	}

	private native void n_openingAnimationWillStart ();

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
