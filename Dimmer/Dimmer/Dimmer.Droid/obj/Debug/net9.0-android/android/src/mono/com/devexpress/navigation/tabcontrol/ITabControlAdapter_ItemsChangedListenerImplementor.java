package mono.com.devexpress.navigation.tabcontrol;


public class ITabControlAdapter_ItemsChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.tabcontrol.ITabControlAdapter.ItemsChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onAddItem:(I)V:GetOnAddItem_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onClearItems:()V:GetOnClearItemsHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onContentChanged:(I)V:GetOnContentChanged_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onContentTemplateChanged:()V:GetOnContentTemplateChangedHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onHeaderContentChanged:(I)V:GetOnHeaderContentChanged_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onHeaderTemplateChanged:()V:GetOnHeaderTemplateChangedHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onRemoveItem:(I)V:GetOnRemoveItem_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"n_onSetItem:(I)V:GetOnSetItem_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter/IItemsChangedListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter+IItemsChangedListenerImplementor, DXNavigation.a", ITabControlAdapter_ItemsChangedListenerImplementor.class, __md_methods);
	}

	public ITabControlAdapter_ItemsChangedListenerImplementor ()
	{
		super ();
		if (getClass () == ITabControlAdapter_ItemsChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapter+IItemsChangedListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onAddItem (int p0)
	{
		n_onAddItem (p0);
	}

	private native void n_onAddItem (int p0);

	public void onClearItems ()
	{
		n_onClearItems ();
	}

	private native void n_onClearItems ();

	public void onContentChanged (int p0)
	{
		n_onContentChanged (p0);
	}

	private native void n_onContentChanged (int p0);

	public void onContentTemplateChanged ()
	{
		n_onContentTemplateChanged ();
	}

	private native void n_onContentTemplateChanged ();

	public void onHeaderContentChanged (int p0)
	{
		n_onHeaderContentChanged (p0);
	}

	private native void n_onHeaderContentChanged (int p0);

	public void onHeaderTemplateChanged ()
	{
		n_onHeaderTemplateChanged ();
	}

	private native void n_onHeaderTemplateChanged ();

	public void onRemoveItem (int p0)
	{
		n_onRemoveItem (p0);
	}

	private native void n_onRemoveItem (int p0);

	public void onSetItem (int p0)
	{
		n_onSetItem (p0);
	}

	private native void n_onSetItem (int p0);

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
