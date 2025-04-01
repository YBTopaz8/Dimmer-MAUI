package crc64222d609bdd44b761;


public class ChipActionImplementation
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.ChipAction,
		com.devexpress.editors.BaseGestureListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCloseIconTap:()Z:GetOnCloseIconTapHandler:DevExpress.Android.Editors.IChipActionInvoker, DXEditors.a\n" +
			"n_onLayoutChanged:()V:GetOnLayoutChangedHandler:DevExpress.Android.Editors.IChipActionInvoker, DXEditors.a\n" +
			"n_onSingleTapConfirmed:()Z:GetOnSingleTapConfirmedHandler:DevExpress.Android.Editors.IChipActionInvoker, DXEditors.a\n" +
			"n_onDoubleTap:()Z:GetOnDoubleTapHandler:DevExpress.Android.Editors.IBaseGestureListenerInvoker, DXEditors.a\n" +
			"n_onLongPress:()Z:GetOnLongPressHandler:DevExpress.Android.Editors.IBaseGestureListenerInvoker, DXEditors.a\n" +
			"n_onSingleTapUp:()Z:GetOnSingleTapUpHandler:DevExpress.Android.Editors.IBaseGestureListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.ChipActionImplementation, DevExpress.Maui.Editors", ChipActionImplementation.class, __md_methods);
	}

	public ChipActionImplementation ()
	{
		super ();
		if (getClass () == ChipActionImplementation.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.ChipActionImplementation, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onCloseIconTap ()
	{
		return n_onCloseIconTap ();
	}

	private native boolean n_onCloseIconTap ();

	public void onLayoutChanged ()
	{
		n_onLayoutChanged ();
	}

	private native void n_onLayoutChanged ();

	public boolean onSingleTapConfirmed ()
	{
		return n_onSingleTapConfirmed ();
	}

	private native boolean n_onSingleTapConfirmed ();

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
