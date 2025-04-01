package crc64222d609bdd44b761;


public class EditBaseHandler_2_EditorGestureListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.EditorGestureListener,
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
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.EditBaseHandler`2+EditorGestureListener, DevExpress.Maui.Editors", EditBaseHandler_2_EditorGestureListener.class, __md_methods);
	}

	public EditBaseHandler_2_EditorGestureListener ()
	{
		super ();
		if (getClass () == EditBaseHandler_2_EditorGestureListener.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.EditBaseHandler`2+EditorGestureListener, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
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
