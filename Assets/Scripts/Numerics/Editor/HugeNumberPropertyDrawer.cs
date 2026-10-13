using System;
using System.Collections.Generic;

using HugeNumberSystem.Numerics.Authoring;

using UnityEditor;
using UnityEngine;

namespace HugeNumberSystem.Numerics.Editor
{
    [CustomPropertyDrawer(typeof(SerializedHugeNumber))]
    public sealed class HugeNumberPropertyDrawer : PropertyDrawer
    {
        private const int BaseRowCount = 3;
        private const int IssueRowCount = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mantissa = property.FindPropertyRelative("_mantissa");
            SerializedProperty exponent = property.FindPropertyRelative("_exponent");
            var raw = new SerializedHugeNumber(mantissa.doubleValue, exponent.longValue);
            IReadOnlyList<HugeNumberAuthoringIssue> issues = GetIssues(property, raw);

            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            bool mixed = property.hasMultipleDifferentValues;

            EditorGUI.BeginProperty(position, label, property);
            bool previousMixedValue = EditorGUI.showMixedValue;
            try
            {
                Rect fields = new Rect(position.x, position.y, position.width, line);
                fields = EditorGUI.PrefixLabel(fields, label);
                float half = (fields.width - gap) * 0.5f;

                EditorGUI.showMixedValue = mixed;
                EditorGUI.PropertyField(
                    new Rect(fields.x, fields.y, half, line), mantissa, GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(fields.x + half + gap, fields.y, half, line), exponent, GUIContent.none);
                EditorGUI.showMixedValue = previousMixedValue;

                Rect preview = new Rect(position.x, position.y + line + gap, position.width, line);
                string previewText = "Multiple values";
                if (!mixed)
                {
                    bool hasError = issues.Count > 0 &&
                        issues[0].Severity == HugeNumberAuthoringIssueSeverity.Error;
                    if (hasError)
                        previewText = "Invalid value";
                    else if (raw.TryToHugeNumber(out HugeNumber normalized, out _))
                        previewText = normalized.ToInvariantString();
                    else
                        previewText = "Invalid value";
                }
                EditorGUI.LabelField(preview, "Normalized", previewText);

                Rect buttons = new Rect(position.x, preview.y + line + gap, position.width, line);
                float buttonHalf = (buttons.width - gap) * 0.5f;
                if (GUI.Button(new Rect(buttons.x, buttons.y, buttonHalf, line), "Copy") &&
                    !mixed && raw.TryToHugeNumber(out _, out _))
                {
                    EditorGUIUtility.systemCopyBuffer = HugeNumberEditorClipboard.Copy(raw);
                }

                if (GUI.Button(
                        new Rect(buttons.x + buttonHalf + gap, buttons.y, buttonHalf, line), "Paste"))
                {
                    if (HugeNumberEditorClipboard.TryPaste(
                            EditorGUIUtility.systemCopyBuffer,
                            out SerializedHugeNumber pasted,
                            out string pasteError))
                    {
                        mantissa.doubleValue = pasted.Mantissa;
                        exponent.longValue = pasted.Exponent;
                    }
                    else
                    {
                        Debug.LogError(pasteError, property.serializedObject.targetObject);
                    }
                }

                if (issues.Count > 0)
                {
                    Rect issueRect = new Rect(
                        position.x,
                        buttons.y + line + gap,
                        position.width,
                        line * IssueRowCount);
                    MessageType type = issues[0].Severity == HugeNumberAuthoringIssueSeverity.Error
                        ? MessageType.Error
                        : MessageType.Warning;
                    EditorGUI.HelpBox(issueRect, issues[0].Message, type);
                }
            }
            finally
            {
                EditorGUI.showMixedValue = previousMixedValue;
                EditorGUI.EndProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty mantissa = property.FindPropertyRelative("_mantissa");
            SerializedProperty exponent = property.FindPropertyRelative("_exponent");
            var raw = new SerializedHugeNumber(mantissa.doubleValue, exponent.longValue);
            int rows = GetIssues(property, raw).Count > 0
                ? BaseRowCount + IssueRowCount
                : BaseRowCount;
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            return line * rows + gap * (rows - 1);
        }

        private static IReadOnlyList<HugeNumberAuthoringIssue> GetIssues(
            SerializedProperty property, SerializedHugeNumber raw)
        {
            if (property.hasMultipleDifferentValues)
                return Array.Empty<HugeNumberAuthoringIssue>();

            return raw.Validate(
                property.serializedObject.targetObject.name,
                property.propertyPath);
        }
    }
}
