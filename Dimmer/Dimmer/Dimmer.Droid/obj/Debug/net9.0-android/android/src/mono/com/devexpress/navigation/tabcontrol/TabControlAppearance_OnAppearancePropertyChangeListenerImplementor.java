package mono.com.devexpress.navigation.tabcontrol;


public class TabControlAppearance_OnAppearancePropertyChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.tabcontrol.TabControlAppearance.OnAppearancePropertyChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onIndicatorPropertyChanged:()V:GetOnIndicatorPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onItemCornerRadiusPropertyChanged:()V:GetOnItemCornerRadiusPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onItemPaddingPropertyChanged:()V:GetOnItemPaddingPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onItemSelectedPropertyChanged:(Lcom/devexpress/navigation/tabs/models/SelectedStyleProperty;)V:GetOnItemSelectedPropertyChanged_Lcom_devexpress_navigation_tabs_models_SelectedStyleProperty_Handler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onItemSpacingPropertyChanged:()V:GetOnItemSpacingPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onItemStylePropertyChanged:(Lcom/devexpress/navigation/tabs/models/StyleProperty;)V:GetOnItemStylePropertyChanged_Lcom_devexpress_navigation_tabs_models_StyleProperty_Handler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onPanelBackgroundPropertyChanged:()V:GetOnPanelBackgroundPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onPanelIndentPropertyChanged:()V:GetOnPanelIndentPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onPanelPaddingPropertyChanged:()V:GetOnPanelPaddingPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"n_onPanelSpacingPropertyChanged:()V:GetOnPanelSpacingPropertyChangedHandler:DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance/IOnAppearancePropertyChangeListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance+IOnAppearancePropertyChangeListenerImplementor, DXNavigation.a", TabControlAppearance_OnAppearancePropertyChangeListenerImplementor.class, __md_methods);
	}

	public TabControlAppearance_OnAppearancePropertyChangeListenerImplementor ()
	{
		super ();
		if (getClass () == TabControlAppearance_OnAppearancePropertyChangeListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.Tabcontrol.TabControlAppearance+IOnAppearancePropertyChangeListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onIndicatorPropertyChanged ()
	{
		n_onIndicatorPropertyChanged ();
	}

	private native void n_onIndicatorPropertyChanged ();

	public void onItemCornerRadiusPropertyChanged ()
	{
		n_onItemCornerRadiusPropertyChanged ();
	}

	private native void n_onItemCornerRadiusPropertyChanged ();

	public void onItemPaddingPropertyChanged ()
	{
		n_onItemPaddingPropertyChanged ();
	}

	private native void n_onItemPaddingPropertyChanged ();

	public void onItemSelectedPropertyChanged (com.devexpress.navigation.tabs.models.SelectedStyleProperty p0)
	{
		n_onItemSelectedPropertyChanged (p0);
	}

	private native void n_onItemSelectedPropertyChanged (com.devexpress.navigation.tabs.models.SelectedStyleProperty p0);

	public void onItemSpacingPropertyChanged ()
	{
		n_onItemSpacingPropertyChanged ();
	}

	private native void n_onItemSpacingPropertyChanged ();

	public void onItemStylePropertyChanged (com.devexpress.navigation.tabs.models.StyleProperty p0)
	{
		n_onItemStylePropertyChanged (p0);
	}

	private native void n_onItemStylePropertyChanged (com.devexpress.navigation.tabs.models.StyleProperty p0);

	public void onPanelBackgroundPropertyChanged ()
	{
		n_onPanelBackgroundPropertyChanged ();
	}

	private native void n_onPanelBackgroundPropertyChanged ();

	public void onPanelIndentPropertyChanged ()
	{
		n_onPanelIndentPropertyChanged ();
	}

	private native void n_onPanelIndentPropertyChanged ();

	public void onPanelPaddingPropertyChanged ()
	{
		n_onPanelPaddingPropertyChanged ();
	}

	private native void n_onPanelPaddingPropertyChanged ();

	public void onPanelSpacingPropertyChanged ()
	{
		n_onPanelSpacingPropertyChanged ();
	}

	private native void n_onPanelSpacingPropertyChanged ();

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
