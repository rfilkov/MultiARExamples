using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(AnchorImageObject))]
public class AnchorImageObjectEditor : PropertyDrawer 
{

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		// Draw label
		//position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var imagePropRect = new Rect(position.x, position.y, position.width - 110, position.height);
		var widthLabelRect = new Rect(position.x + position.width - 100, position.y, 40, position.height);
		var widthPropRect = new Rect(position.x + position.width - 60, position.y, 60, position.height);

		var labelStyle = new GUIStyle(GUI.skin.label);
		labelStyle.alignment = TextAnchor.MiddleRight;

		// Draw fields - pass GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(imagePropRect, property.FindPropertyRelative("image"), GUIContent.none);
		EditorGUI.LabelField(widthLabelRect, "W(m):", labelStyle);
		EditorGUI.PropertyField(widthPropRect, property.FindPropertyRelative("width"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}

}
