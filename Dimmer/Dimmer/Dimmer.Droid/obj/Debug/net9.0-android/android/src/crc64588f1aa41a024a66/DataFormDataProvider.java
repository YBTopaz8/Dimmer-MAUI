package crc64588f1aa41a024a66;


public class DataFormDataProvider
	extends com.devexpress.editors.dataForm.protocols.DXDataFormDataProvider
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getEditors:(I)Ljava/util/List;:GetGetEditors_IHandler\n" +
			"n_getGroups:()Ljava/util/List;:GetGetGroupsHandler\n" +
			"n_isLastElementInLine:(II)Z:GetIsLastElementInLine_IIHandler\n" +
			"n_getEditor:(II)Lcom/devexpress/editors/dataForm/protocols/DataFormEditorInfo;:GetGetEditor_IIHandler\n" +
			"n_getGroup:(I)Lcom/devexpress/editors/dataForm/protocols/ExpanderInfo;:GetGetGroup_IHandler\n" +
			"n_getPickerDataSource:(II)Ljava/util/List;:GetGetPickerDataSource_IIHandler\n" +
			"n_isSourceUpdated:(II)Z:GetIsSourceUpdated_IIHandler\n" +
			"n_postValue:(Ljava/lang/Object;II)V:GetPostValue_Ljava_lang_Object_IIHandler\n" +
			"n_validate:(Ljava/lang/Object;II)Lcom/devexpress/editors/dataForm/protocols/DataFormValueValidationError;:GetValidate_Ljava_lang_Object_IIHandler\n" +
			"n_preValidate:(Ljava/lang/Object;II)Lcom/devexpress/editors/dataForm/protocols/DataFormValueValidationError;:GetPreValidate_Ljava_lang_Object_IIHandler\n" +
			"n_postValidate:(II)Lcom/devexpress/editors/dataForm/protocols/DataFormValueValidationError;:GetPostValidate_IIHandler\n" +
			"n_validateValues:(Ljava/util/Map;)Lcom/devexpress/editors/dataForm/protocols/DataFormValuesValidationErrors;:GetValidateValues_Ljava_util_Map_Handler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataForm.Internal.DataFormDataProvider, DevExpress.Maui.Editors", DataFormDataProvider.class, __md_methods);
	}

	public DataFormDataProvider ()
	{
		super ();
		if (getClass () == DataFormDataProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormDataProvider, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public java.util.List getEditors (int p0)
	{
		return n_getEditors (p0);
	}

	private native java.util.List n_getEditors (int p0);

	public java.util.List getGroups ()
	{
		return n_getGroups ();
	}

	private native java.util.List n_getGroups ();

	public boolean isLastElementInLine (int p0, int p1)
	{
		return n_isLastElementInLine (p0, p1);
	}

	private native boolean n_isLastElementInLine (int p0, int p1);

	public com.devexpress.editors.dataForm.protocols.DataFormEditorInfo getEditor (int p0, int p1)
	{
		return n_getEditor (p0, p1);
	}

	private native com.devexpress.editors.dataForm.protocols.DataFormEditorInfo n_getEditor (int p0, int p1);

	public com.devexpress.editors.dataForm.protocols.ExpanderInfo getGroup (int p0)
	{
		return n_getGroup (p0);
	}

	private native com.devexpress.editors.dataForm.protocols.ExpanderInfo n_getGroup (int p0);

	public java.util.List getPickerDataSource (int p0, int p1)
	{
		return n_getPickerDataSource (p0, p1);
	}

	private native java.util.List n_getPickerDataSource (int p0, int p1);

	public boolean isSourceUpdated (int p0, int p1)
	{
		return n_isSourceUpdated (p0, p1);
	}

	private native boolean n_isSourceUpdated (int p0, int p1);

	public void postValue (java.lang.Object p0, int p1, int p2)
	{
		n_postValue (p0, p1, p2);
	}

	private native void n_postValue (java.lang.Object p0, int p1, int p2);

	public com.devexpress.editors.dataForm.protocols.DataFormValueValidationError validate (java.lang.Object p0, int p1, int p2)
	{
		return n_validate (p0, p1, p2);
	}

	private native com.devexpress.editors.dataForm.protocols.DataFormValueValidationError n_validate (java.lang.Object p0, int p1, int p2);

	public com.devexpress.editors.dataForm.protocols.DataFormValueValidationError preValidate (java.lang.Object p0, int p1, int p2)
	{
		return n_preValidate (p0, p1, p2);
	}

	private native com.devexpress.editors.dataForm.protocols.DataFormValueValidationError n_preValidate (java.lang.Object p0, int p1, int p2);

	public com.devexpress.editors.dataForm.protocols.DataFormValueValidationError postValidate (int p0, int p1)
	{
		return n_postValidate (p0, p1);
	}

	private native com.devexpress.editors.dataForm.protocols.DataFormValueValidationError n_postValidate (int p0, int p1);

	public com.devexpress.editors.dataForm.protocols.DataFormValuesValidationErrors validateValues (java.util.Map p0)
	{
		return n_validateValues (p0);
	}

	private native com.devexpress.editors.dataForm.protocols.DataFormValuesValidationErrors n_validateValues (java.util.Map p0);

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
