using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer (typeof (PowRangeAttribute))]
public class PowRangeDrawer : PropertyDrawer
{
	PowRangeAttribute att { get { return ((PowRangeAttribute)attribute); } }

	// Here you can define the GUI for your property drawer. Called by Unity.
	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		if (att.linear)
			DrawLinear(position, prop);
		else
			DrawExp(position, prop);
	}

	public void DrawLinear (Rect position, SerializedProperty prop)
	{
		Rect rect = EditorGUI.PrefixLabel(position, new GUIContent(string.Format("{0}: {1}", prop.name, prop.intValue)));
		position.xMin += rect.width;
		position.xMax -= 5;
		int val = Mathf.RoundToInt(GUI.HorizontalSlider(position, att.GetExp(prop.intValue), att.GetExp(att.min), att.GetExp(att.max)));
		prop.intValue = (val > 0) ? Mathf.RoundToInt(Mathf.Pow(att.pow, val)) : 0;
	}

	public void DrawExp (Rect position, SerializedProperty prop)
	{
		int val = att.RoundToPow(prop.intValue);
		Rect rect = EditorGUI.PrefixLabel(position, new GUIContent(string.Format("{0}: {1}", prop.name, val)));
		position.xMin += rect.width;
		position.xMax -= 5;
		val = Mathf.RoundToInt(GUI.HorizontalSlider(position, val, att.min, att.max));
		prop.intValue = att.RoundToPow(val);
	}
}