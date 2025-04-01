package mono.com.devexpress.dxlistview.core;


public class GestureInteractionListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxlistview.core.GestureInteractionListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_down:(Landroid/view/MotionEvent;)V:GetDown_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"n_move:(Landroid/view/MotionEvent;)Z:GetMove_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"n_onDoubleTap:(Landroid/view/MotionEvent;)Z:GetOnDoubleTap_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"n_onLongPress:(Landroid/view/MotionEvent;)V:GetOnLongPress_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"n_onSingleTapConfirmed:(Landroid/view/MotionEvent;)Z:GetOnSingleTapConfirmed_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"n_onSingleTapUp:(Landroid/view/MotionEvent;)Z:GetOnSingleTapUp_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"n_up:(Landroid/view/MotionEvent;)Z:GetUp_Landroid_view_MotionEvent_Handler:DevExpress.Android.CollectionView.Core.IGestureInteractionListenerInvoker, DXCollectionView.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.CollectionView.Core.IGestureInteractionListenerImplementor, DXCollectionView.a", GestureInteractionListenerImplementor.class, __md_methods);
	}

	public GestureInteractionListenerImplementor ()
	{
		super ();
		if (getClass () == GestureInteractionListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.CollectionView.Core.IGestureInteractionListenerImplementor, DXCollectionView.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void down (android.view.MotionEvent p0)
	{
		n_down (p0);
	}

	private native void n_down (android.view.MotionEvent p0);

	public boolean move (android.view.MotionEvent p0)
	{
		return n_move (p0);
	}

	private native boolean n_move (android.view.MotionEvent p0);

	public boolean onDoubleTap (android.view.MotionEvent p0)
	{
		return n_onDoubleTap (p0);
	}

	private native boolean n_onDoubleTap (android.view.MotionEvent p0);

	public void onLongPress (android.view.MotionEvent p0)
	{
		n_onLongPress (p0);
	}

	private native void n_onLongPress (android.view.MotionEvent p0);

	public boolean onSingleTapConfirmed (android.view.MotionEvent p0)
	{
		return n_onSingleTapConfirmed (p0);
	}

	private native boolean n_onSingleTapConfirmed (android.view.MotionEvent p0);

	public boolean onSingleTapUp (android.view.MotionEvent p0)
	{
		return n_onSingleTapUp (p0);
	}

	private native boolean n_onSingleTapUp (android.view.MotionEvent p0);

	public boolean up (android.view.MotionEvent p0)
	{
		return n_up (p0);
	}

	private native boolean n_up (android.view.MotionEvent p0);

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
