package mono.com.devexpress.navigation.tabs.models;


public class TabItemSettings_OnSettingsChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.tabs.models.TabItemSettings.OnSettingsChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onHeaderHeightOnVerticalPanelChanged:(Ljava/lang/Object;Lcom/devexpress/navigation/tabs/models/TabSize;Lcom/devexpress/navigation/tabs/models/TabSize;)V:GetOnHeaderHeightOnVerticalPanelChanged_Ljava_lang_Object_Lcom_devexpress_navigation_tabs_models_TabSize_Lcom_devexpress_navigation_tabs_models_TabSize_Handler:DevExpress.Android.Navigation.Tabs.Models.TabItemSettings/IOnSettingsChangeListenerInvoker, DXNavigation.a\n" +
			"n_onHeaderIconPositionChanged:(Lcom/devexpress/navigation/common/Position;Lcom/devexpress/navigation/common/Position;)V:GetOnHeaderIconPositionChanged_Lcom_devexpress_navigation_common_Position_Lcom_devexpress_navigation_common_Position_Handler:DevExpress.Android.Navigation.Tabs.Models.TabItemSettings/IOnSettingsChangeListenerInvoker, DXNavigation.a\n" +
			"n_onHeaderVisibleElementsChanged:(Lcom/devexpress/navigation/common/HeaderElements;Lcom/devexpress/navigation/common/HeaderElements;)V:GetOnHeaderVisibleElementsChanged_Lcom_devexpress_navigation_common_HeaderElements_Lcom_devexpress_navigation_common_HeaderElements_Handler:DevExpress.Android.Navigation.Tabs.Models.TabItemSettings/IOnSettingsChangeListenerInvoker, DXNavigation.a\n" +
			"n_onHeaderWidthOnHorizontalPanelChanged:(Ljava/lang/Object;Lcom/devexpress/navigation/tabs/models/TabSize;Lcom/devexpress/navigation/tabs/models/TabSize;)V:GetOnHeaderWidthOnHorizontalPanelChanged_Ljava_lang_Object_Lcom_devexpress_navigation_tabs_models_TabSize_Lcom_devexpress_navigation_tabs_models_TabSize_Handler:DevExpress.Android.Navigation.Tabs.Models.TabItemSettings/IOnSettingsChangeListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.Tabs.Models.TabItemSettings+IOnSettingsChangeListenerImplementor, DXNavigation.a", TabItemSettings_OnSettingsChangeListenerImplementor.class, __md_methods);
	}

	public TabItemSettings_OnSettingsChangeListenerImplementor ()
	{
		super ();
		if (getClass () == TabItemSettings_OnSettingsChangeListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.Tabs.Models.TabItemSettings+IOnSettingsChangeListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onHeaderHeightOnVerticalPanelChanged (java.lang.Object p0, com.devexpress.navigation.tabs.models.TabSize p1, com.devexpress.navigation.tabs.models.TabSize p2)
	{
		n_onHeaderHeightOnVerticalPanelChanged (p0, p1, p2);
	}

	private native void n_onHeaderHeightOnVerticalPanelChanged (java.lang.Object p0, com.devexpress.navigation.tabs.models.TabSize p1, com.devexpress.navigation.tabs.models.TabSize p2);

	public void onHeaderIconPositionChanged (com.devexpress.navigation.common.Position p0, com.devexpress.navigation.common.Position p1)
	{
		n_onHeaderIconPositionChanged (p0, p1);
	}

	private native void n_onHeaderIconPositionChanged (com.devexpress.navigation.common.Position p0, com.devexpress.navigation.common.Position p1);

	public void onHeaderVisibleElementsChanged (com.devexpress.navigation.common.HeaderElements p0, com.devexpress.navigation.common.HeaderElements p1)
	{
		n_onHeaderVisibleElementsChanged (p0, p1);
	}

	private native void n_onHeaderVisibleElementsChanged (com.devexpress.navigation.common.HeaderElements p0, com.devexpress.navigation.common.HeaderElements p1);

	public void onHeaderWidthOnHorizontalPanelChanged (java.lang.Object p0, com.devexpress.navigation.tabs.models.TabSize p1, com.devexpress.navigation.tabs.models.TabSize p2)
	{
		n_onHeaderWidthOnHorizontalPanelChanged (p0, p1, p2);
	}

	private native void n_onHeaderWidthOnHorizontalPanelChanged (java.lang.Object p0, com.devexpress.navigation.tabs.models.TabSize p1, com.devexpress.navigation.tabs.models.TabSize p2);

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
