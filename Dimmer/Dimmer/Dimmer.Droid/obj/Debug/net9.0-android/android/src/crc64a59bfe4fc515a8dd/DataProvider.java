package crc64a59bfe4fc515a8dd;


public class DataProvider
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.DataProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getRowCount:()I:GetGetRowCountHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getCellErrorText:(Ljava/lang/String;I)Ljava/lang/String;:GetGetCellErrorText_Ljava_lang_String_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getDisplayText:(Ljava/lang/Object;Ljava/lang/String;I)Ljava/lang/String;:GetGetDisplayText_Ljava_lang_Object_Ljava_lang_String_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getDisplayText:(Ljava/lang/String;I)Ljava/lang/String;:GetGetDisplayText_Ljava_lang_String_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getGroupInfo:(I)Lcom/devexpress/dxgrid/models/GroupInfo;:GetGetGroupInfo_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getTotalSummary:(I)Ljava/lang/String;:GetGetTotalSummary_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getValue:(Ljava/lang/String;I)Ljava/lang/Object;:GetGetValue_Ljava_lang_String_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_getValueAsync:(Ljava/lang/String;ILcom/devexpress/dxgrid/utils/ObjectLambda;)V:GetGetValueAsync_Ljava_lang_String_ILcom_devexpress_dxgrid_utils_ObjectLambda_Handler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_isGroupRow:(I)Z:GetIsGroupRow_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_isSelected:(I)Z:GetIsSelected_IHandler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"n_setCellValue:(Ljava/lang/String;ILjava/lang/Object;)Ljava/lang/String;:GetSetCellValue_Ljava_lang_String_ILjava_lang_Object_Handler:DevExpress.Android.Grid.Providers.IDataProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.DataProvider, DevExpress.Maui.DataGrid", DataProvider.class, __md_methods);
	}

	public DataProvider ()
	{
		super ();
		if (getClass () == DataProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.DataProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public int getRowCount ()
	{
		return n_getRowCount ();
	}

	private native int n_getRowCount ();

	public java.lang.String getCellErrorText (java.lang.String p0, int p1)
	{
		return n_getCellErrorText (p0, p1);
	}

	private native java.lang.String n_getCellErrorText (java.lang.String p0, int p1);

	public java.lang.String getDisplayText (java.lang.Object p0, java.lang.String p1, int p2)
	{
		return n_getDisplayText (p0, p1, p2);
	}

	private native java.lang.String n_getDisplayText (java.lang.Object p0, java.lang.String p1, int p2);

	public java.lang.String getDisplayText (java.lang.String p0, int p1)
	{
		return n_getDisplayText (p0, p1);
	}

	private native java.lang.String n_getDisplayText (java.lang.String p0, int p1);

	public com.devexpress.dxgrid.models.GroupInfo getGroupInfo (int p0)
	{
		return n_getGroupInfo (p0);
	}

	private native com.devexpress.dxgrid.models.GroupInfo n_getGroupInfo (int p0);

	public java.lang.String getTotalSummary (int p0)
	{
		return n_getTotalSummary (p0);
	}

	private native java.lang.String n_getTotalSummary (int p0);

	public java.lang.Object getValue (java.lang.String p0, int p1)
	{
		return n_getValue (p0, p1);
	}

	private native java.lang.Object n_getValue (java.lang.String p0, int p1);

	public void getValueAsync (java.lang.String p0, int p1, com.devexpress.dxgrid.utils.ObjectLambda p2)
	{
		n_getValueAsync (p0, p1, p2);
	}

	private native void n_getValueAsync (java.lang.String p0, int p1, com.devexpress.dxgrid.utils.ObjectLambda p2);

	public boolean isGroupRow (int p0)
	{
		return n_isGroupRow (p0);
	}

	private native boolean n_isGroupRow (int p0);

	public boolean isSelected (int p0)
	{
		return n_isSelected (p0);
	}

	private native boolean n_isSelected (int p0);

	public java.lang.String setCellValue (java.lang.String p0, int p1, java.lang.Object p2)
	{
		return n_setCellValue (p0, p1, p2);
	}

	private native java.lang.String n_setCellValue (java.lang.String p0, int p1, java.lang.Object p2);

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
