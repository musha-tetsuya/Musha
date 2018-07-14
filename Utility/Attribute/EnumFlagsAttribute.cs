using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class EnumFlagsAttribute : PropertyAttribute
{
	public Type enumType = null;

	public EnumFlagsAttribute(Type _enumType = null)
	{
		this.enumType = _enumType;
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
public sealed class EnumFlagsAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var enumType = (this.attribute as EnumFlagsAttribute).enumType;

		string[] displayedOptions = (enumType == null) ? property.enumNames : Enum.GetNames(enumType);

		property.intValue = EditorGUI.MaskField(position, label, property.intValue, displayedOptions);
	}
}
#endif