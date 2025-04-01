package mono.com.devexpress.editors.utils;


public class CheckableImageButton_OnCheckedChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.utils.CheckableImageButton.OnCheckedChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCheckedChanged:(Landroid/widget/Checkable;Z)V:GetOnCheckedChanged_Landroid_widget_Checkable_ZHandler:DevExpress.Android.Editors.Util.CheckableImageButton/IOnCheckedChangeListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.Util.CheckableImageButton+IOnCheckedChangeListenerImplementor, DXEditors.a", CheckableImageButton_OnCheckedChangeListenerImplementor.class, __md_methods);
	}

	public CheckableImageButton_OnCheckedChangeListenerImplementor ()
	{
		super ();
		if (getClass () == CheckableImageButton_OnCheckedChangeListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.Util.CheckableImageButton+IOnCheckedChangeListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onCheckedChanged (android.widget.Checkable p0, boolean p1)
	{
		n_onCheckedChanged (p0, p1);
	}

	private native void n_onCheckedChanged (android.widget.Checkable p0, boolean p1);

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
