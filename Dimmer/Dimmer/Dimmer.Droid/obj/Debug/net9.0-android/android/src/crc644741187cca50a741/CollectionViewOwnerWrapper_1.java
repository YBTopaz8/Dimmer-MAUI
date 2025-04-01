package crc644741187cca50a741;


public abstract class CollectionViewOwnerWrapper_1
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.popupmanagers.CollectionViewOwner
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getCollectionView:()Landroid/view/View;:GetGetCollectionViewHandler:DevExpress.Android.Editors.Popupmanagers.ICollectionViewOwnerInvoker, DXEditors.a\n" +
			"n_hasValue:()Z:GetHasValueHandler:DevExpress.Android.Editors.Popupmanagers.ICollectionViewOwnerInvoker, DXEditors.a\n" +
			"n_isDataSourceEmpty:()Z:GetIsDataSourceEmptyHandler:DevExpress.Android.Editors.Popupmanagers.ICollectionViewOwnerInvoker, DXEditors.a\n" +
			"n_getSelectedItemText:()Ljava/lang/CharSequence;:GetGetSelectedItemTextHandler:DevExpress.Android.Editors.Popupmanagers.ICollectionViewOwnerInvoker, DXEditors.a\n" +
			"n_clearValue:()V:GetClearValueHandler:DevExpress.Android.Editors.Popupmanagers.ICollectionViewOwnerInvoker, DXEditors.a\n" +
			"n_ensureSelectionVisible:()V:GetEnsureSelectionVisibleHandler:DevExpress.Android.Editors.Popupmanagers.ICollectionViewOwnerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Android.Internal.CollectionViewOwnerWrapper`1, DevExpress.Maui.Editors", CollectionViewOwnerWrapper_1.class, __md_methods);
	}

	public CollectionViewOwnerWrapper_1 ()
	{
		super ();
		if (getClass () == CollectionViewOwnerWrapper_1.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Android.Internal.CollectionViewOwnerWrapper`1, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public android.view.View getCollectionView ()
	{
		return n_getCollectionView ();
	}

	private native android.view.View n_getCollectionView ();

	public boolean hasValue ()
	{
		return n_hasValue ();
	}

	private native boolean n_hasValue ();

	public boolean isDataSourceEmpty ()
	{
		return n_isDataSourceEmpty ();
	}

	private native boolean n_isDataSourceEmpty ();

	public java.lang.CharSequence getSelectedItemText ()
	{
		return n_getSelectedItemText ();
	}

	private native java.lang.CharSequence n_getSelectedItemText ();

	public void clearValue ()
	{
		n_clearValue ();
	}

	private native void n_clearValue ();

	public void ensureSelectionVisible ()
	{
		n_ensureSelectionVisible ();
	}

	private native void n_ensureSelectionVisible ();

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
