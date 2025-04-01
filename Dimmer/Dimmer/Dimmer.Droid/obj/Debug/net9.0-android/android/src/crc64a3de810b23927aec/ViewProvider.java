package crc64a3de810b23927aec;


public class ViewProvider
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxlistview.core.DXListItemViewProvider,
		com.devexpress.dxlistview.swipes.DXSwipeItemsViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_isItemsSourceGrouped:()Z:GetIsItemsSourceGroupedHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getItemCount:()I:GetGetItemCountHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_calculateNodePosition:(I)I:GetCalculateNodePosition_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_checkView:(Landroid/view/View;I)Z:GetCheckView_Landroid_view_View_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_clearCache:()V:GetClearCacheHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getEndVisibleIndexInNodeByItem:(I)I:GetGetEndVisibleIndexInNodeByItem_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getGroupItemIndexForItem:(I)I:GetGetGroupItemIndexForItem_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getStartVisibleIndexInNode:(I)I:GetGetStartVisibleIndexInNode_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getStartVisibleIndexInNodeByItem:(I)I:GetGetStartVisibleIndexInNodeByItem_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getViewByIndex:(II)Landroid/view/View;:GetGetViewByIndex_IIHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getViewTypeByIndex:(I)I:GetGetViewTypeByIndex_IHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_recycleView:(Landroid/view/View;II)V:GetRecycleView_Landroid_view_View_IIHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_updateView:(Landroid/view/View;IIZ)V:GetUpdateView_Landroid_view_View_IIZHandler:DevExpress.Android.CollectionView.Core.IDXListItemViewProviderInvoker, DXCollectionView.a\n" +
			"n_getAllowFullSwipe:(ILcom/devexpress/dxlistview/swipes/DXSwipeGroup;)Ljava/lang/Boolean;:GetGetAllowFullSwipe_ILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Handler:DevExpress.Android.CollectionView.Swipes.IDXSwipeItemsViewProviderInvoker, DXCollectionView.a\n" +
			"n_getSwipeViewColors:(ILcom/devexpress/dxlistview/swipes/DXSwipeGroup;)Ljava/util/List;:GetGetSwipeViewColors_ILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Handler:DevExpress.Android.CollectionView.Swipes.IDXSwipeItemsViewProviderInvoker, DXCollectionView.a\n" +
			"n_getSwipeViewSizes:(ILcom/devexpress/dxlistview/swipes/DXSwipeGroup;)Ljava/util/List;:GetGetSwipeViewSizes_ILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Handler:DevExpress.Android.CollectionView.Swipes.IDXSwipeItemsViewProviderInvoker, DXCollectionView.a\n" +
			"n_getSwipeViews:(ILcom/devexpress/dxlistview/swipes/DXSwipeGroup;)Ljava/util/List;:GetGetSwipeViews_ILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Handler:DevExpress.Android.CollectionView.Swipes.IDXSwipeItemsViewProviderInvoker, DXCollectionView.a\n" +
			"n_isSwipeAllowed:(ILcom/devexpress/dxlistview/swipes/DXSwipeGroup;)Ljava/lang/Boolean;:GetIsSwipeAllowed_ILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Handler:DevExpress.Android.CollectionView.Swipes.IDXSwipeItemsViewProviderInvoker, DXCollectionView.a\n" +
			"n_recycleViews:(ILcom/devexpress/dxlistview/swipes/DXSwipeGroup;Ljava/util/List;)V:GetRecycleViews_ILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Ljava_util_List_Handler:DevExpress.Android.CollectionView.Swipes.IDXSwipeItemsViewProviderInvoker, DXCollectionView.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.CollectionView.Android.Internal.ViewProvider, DevExpress.Maui.CollectionView", ViewProvider.class, __md_methods);
	}

	public ViewProvider ()
	{
		super ();
		if (getClass () == ViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.CollectionView.Android.Internal.ViewProvider, DevExpress.Maui.CollectionView", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean isItemsSourceGrouped ()
	{
		return n_isItemsSourceGrouped ();
	}

	private native boolean n_isItemsSourceGrouped ();

	public int getItemCount ()
	{
		return n_getItemCount ();
	}

	private native int n_getItemCount ();

	public int calculateNodePosition (int p0)
	{
		return n_calculateNodePosition (p0);
	}

	private native int n_calculateNodePosition (int p0);

	public boolean checkView (android.view.View p0, int p1)
	{
		return n_checkView (p0, p1);
	}

	private native boolean n_checkView (android.view.View p0, int p1);

	public void clearCache ()
	{
		n_clearCache ();
	}

	private native void n_clearCache ();

	public int getEndVisibleIndexInNodeByItem (int p0)
	{
		return n_getEndVisibleIndexInNodeByItem (p0);
	}

	private native int n_getEndVisibleIndexInNodeByItem (int p0);

	public int getGroupItemIndexForItem (int p0)
	{
		return n_getGroupItemIndexForItem (p0);
	}

	private native int n_getGroupItemIndexForItem (int p0);

	public int getStartVisibleIndexInNode (int p0)
	{
		return n_getStartVisibleIndexInNode (p0);
	}

	private native int n_getStartVisibleIndexInNode (int p0);

	public int getStartVisibleIndexInNodeByItem (int p0)
	{
		return n_getStartVisibleIndexInNodeByItem (p0);
	}

	private native int n_getStartVisibleIndexInNodeByItem (int p0);

	public android.view.View getViewByIndex (int p0, int p1)
	{
		return n_getViewByIndex (p0, p1);
	}

	private native android.view.View n_getViewByIndex (int p0, int p1);

	public int getViewTypeByIndex (int p0)
	{
		return n_getViewTypeByIndex (p0);
	}

	private native int n_getViewTypeByIndex (int p0);

	public void recycleView (android.view.View p0, int p1, int p2)
	{
		n_recycleView (p0, p1, p2);
	}

	private native void n_recycleView (android.view.View p0, int p1, int p2);

	public void updateView (android.view.View p0, int p1, int p2, boolean p3)
	{
		n_updateView (p0, p1, p2, p3);
	}

	private native void n_updateView (android.view.View p0, int p1, int p2, boolean p3);

	public java.lang.Boolean getAllowFullSwipe (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1)
	{
		return n_getAllowFullSwipe (p0, p1);
	}

	private native java.lang.Boolean n_getAllowFullSwipe (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1);

	public java.util.List getSwipeViewColors (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1)
	{
		return n_getSwipeViewColors (p0, p1);
	}

	private native java.util.List n_getSwipeViewColors (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1);

	public java.util.List getSwipeViewSizes (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1)
	{
		return n_getSwipeViewSizes (p0, p1);
	}

	private native java.util.List n_getSwipeViewSizes (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1);

	public java.util.List getSwipeViews (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1)
	{
		return n_getSwipeViews (p0, p1);
	}

	private native java.util.List n_getSwipeViews (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1);

	public java.lang.Boolean isSwipeAllowed (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1)
	{
		return n_isSwipeAllowed (p0, p1);
	}

	private native java.lang.Boolean n_isSwipeAllowed (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1);

	public void recycleViews (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1, java.util.List p2)
	{
		n_recycleViews (p0, p1, p2);
	}

	private native void n_recycleViews (int p0, com.devexpress.dxlistview.swipes.DXSwipeGroup p1, java.util.List p2);

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
