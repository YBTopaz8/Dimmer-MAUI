package mono.com.devexpress.editors;


public class BaseGestureListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.BaseGestureListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onDoubleTap:()Z:GetOnDoubleTapHandler:DevExpress.Android.Editors.IBaseGestureListenerInvoker, DXEditors.a\n" +
			"n_onLongPress:()Z:GetOnLongPressHandler:DevExpress.Android.Editors.IBaseGestureListenerInvoker, DXEditors.a\n" +
			"n_onSingleTapUp:()Z:GetOnSingleTapUpHandler:DevExpress.Android.Editors.IBaseGestureListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.IBaseGestureListenerImplementor, DXEditors.a", BaseGestureListenerImplementor.class, __md_methods);
	}

	public BaseGestureListenerImplementor ()
	{
		super ();
		if (getClass () == BaseGestureListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.IBaseGestureListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onDoubleTap ()
	{
		return n_onDoubleTap ();
	}

	private native boolean n_onDoubleTap ();

	public boolean onLongPress ()
	{
		return n_onLongPress ();
	}

	private native boolean n_onLongPress ();

	public boolean onSingleTapUp ()
	{
		return n_onSingleTapUp ();
	}

	private native boolean n_onSingleTapUp ();

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
