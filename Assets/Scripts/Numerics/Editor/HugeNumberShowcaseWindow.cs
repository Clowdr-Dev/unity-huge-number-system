using System;
using System.Globalization;
using HugeNumberSystem.Numerics.Authoring;
using HugeNumberSystem.Numerics.Persistence;
using HugeNumberSystem.Numerics.Presentation;
using UnityEditor;
using UnityEngine;

namespace HugeNumberSystem.Numerics.Editor
{
    /// <summary>A reproducible, interactive view of the library's public APIs.</summary>
    public sealed class HugeNumberShowcaseWindow : EditorWindow
    {
        private static readonly Color Background = new Color(0.055f, 0.075f, 0.11f);
        private static readonly Color Panel = new Color(0.09f, 0.12f, 0.17f);
        private static readonly Color Accent = new Color(1f, 0.57f, 0.25f);
        [SerializeField] private string _input = "1.25e12000";
        [SerializeField] private SerializedHugeNumber _authored = new SerializedHugeNumber(1.25d, 12000L);
        private SerializedObject _serialized;
        private SerializedProperty _property;
        private readonly HugeNumberFormatter _formatter = new HugeNumberFormatter();
        private GUIStyle _title, _hero, _heading, _body, _muted, _code, _inputStyle;
        private string _error;
        private HugeNumber _value;
        private Vector2 _scroll;

        [MenuItem("Huge Number System/Showcase")]
        public static void Open()
        {
            var window = GetWindow<HugeNumberShowcaseWindow>();
            window.titleContent = new GUIContent("Huge Number System");
            window.minSize = new Vector2(850, 720);
            window.Show();
        }

        private void OnEnable()
        {
            _serialized = new SerializedObject(this);
            _property = _serialized.FindProperty("_authored");
            ParseInput();
        }

        private void OnDisable()
        {
            _serialized?.Dispose();
            _serialized = null;
            _property = null;
        }

        private void ParseInput()
        {
            if (HugeNumber.TryParseInvariant(_input, out var value))
            {
                _value = value;
                _error = null;
            }
            else _error = "Use an invariant number such as 1.25e12000.";
        }

        private void OnGUI()
        {
            EnsureStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), Background);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.BeginVertical();
            GUILayout.Space(24);
            GUILayout.BeginHorizontal();
            GUILayout.Space(28);
            GUILayout.BeginVertical();
            GUILayout.Label("CLOWDR DEV    /    UNITY + C#    /    SOURCE PREVIEW", _muted);
            GUILayout.Space(10);
            GUILayout.Label("Huge numbers. Clear contracts.", _title);
            GUILayout.Label("Arithmetic beyond double range, with explicit precision and overflow policies.", _body);
            GUILayout.Space(20);

            BeginPanel();
            GUILayout.Label("EXPLORE A VALUE", _heading);
            EditorGUI.BeginChangeCheck();
            _input = EditorGUILayout.TextField(_input, _inputStyle, GUILayout.Height(34));
            if (EditorGUI.EndChangeCheck()) ParseInput();
            if (_error != null) EditorGUILayout.HelpBox(_error, MessageType.Warning);
            GUILayout.Label(_value.ToInvariantString(), _hero);
            GUILayout.Label("double mantissa  x  10 ^ signed 64-bit exponent", _muted);
            GUILayout.EndVertical();
            GUILayout.Space(14);

            GUILayout.BeginHorizontal();
            BeginPanel(true);
            GUILayout.Label("01   CALCULATE", _heading);
            var cost = HugeNumber.FromParts(5d, 11999L);
            ValueRow("Balance", _value.ToInvariantString());
            ValueRow("Upgrade cost", cost.ToInvariantString());
            ValueRow("Remaining", (_value - cost).ToInvariantString());
            ValueRow("Affordable", _value >= cost ? "Yes" : "No");
            GUILayout.Space(5);
            GUILayout.Label("Compare complete values; keep display rounding in the UI.", _muted);
            GUILayout.EndVertical();
            GUILayout.Space(14);
            BeginPanel(true);
            GUILayout.Label("02   FORMAT", _heading);
            ValueRow("Scientific", Format(_value, HugeNumberNotation.Scientific));
            ValueRow("Engineering", Format(_value, HugeNumberNotation.Engineering));
            ValueRow("Compact example", Format(HugeNumber.ParseInvariant("1.25e6"), HugeNumberNotation.Compact));
            ValueRow("Round-trip text", _value.ToInvariantString());
            GUILayout.Space(5);
            GUILayout.Label("Compact, scientific, and engineering notation; invariant persistence.", _muted);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            GUILayout.BeginHorizontal();
            BeginPanel(true);
            GUILayout.Label("03   SAVE & RESTORE", _heading);
            var data = HugeNumberPersistence.Write(_value);
            var json = JsonUtility.ToJson(data, true);
            GUILayout.Label(json, _code);
            var restored = JsonUtility.FromJson<HugeNumberSaveData>(json);
            var matches = HugeNumberPersistence.TryRead(restored, out var read, out _) && read == _value;
            GUILayout.Label(matches ? "ROUND TRIP MATCHES" : "RESTORE FAILED", _heading);
            GUILayout.Label("Versioned fields; no culture-dependent save strings.", _muted);
            GUILayout.EndVertical();
            GUILayout.Space(14);
            BeginPanel(true);
            GUILayout.Label("04   AUTHOR IN UNITY", _heading);
            GUILayout.Space(6);
            _serialized.Update();
            EditorGUILayout.PropertyField(_property, new GUIContent("Value"), true);
            _serialized.ApplyModifiedProperties();
            GUILayout.Space(12);
            GUILayout.Label("The same serialized bridge and Inspector drawer used by your game assets.", _muted);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(14);
            GUILayout.Label("~15 significant digits   /   Approximate arithmetic   /   PolyForm Noncommercial 1.0.0", _muted);
            GUILayout.EndVertical();
            GUILayout.Space(28);
            GUILayout.EndHorizontal();
            GUILayout.Space(24);
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private string Format(HugeNumber value, HugeNumberNotation notation)
            => _formatter.Format(value, new HugeNumberFormatOptions(notation, CultureInfo.InvariantCulture));

        private void ValueRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _muted, GUILayout.Width(128));
            GUILayout.Label(value, _body);
            GUILayout.EndHorizontal();
        }

        private GUIStyle _panel;
        private void BeginPanel(bool halfWidth = false)
        {
            var rect = halfWidth
                ? EditorGUILayout.BeginVertical(_panel, GUILayout.Width((position.width - 74) / 2))
                : EditorGUILayout.BeginVertical(_panel);
            EditorGUI.DrawRect(rect, Panel);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = Label(32, Color.white, true);
            _hero = Label(52, Accent, true);
            _heading = Label(13, Accent, true);
            _body = Label(16, new Color(0.88f, 0.92f, 0.97f));
            _muted = Label(12, new Color(0.64f, 0.72f, 0.82f));
            _code = Label(14, new Color(0.78f, 0.86f, 0.95f));
            _inputStyle = new GUIStyle(EditorStyles.textField) { fontSize = 18, padding = new RectOffset(10, 10, 5, 5) };
            _panel = new GUIStyle { padding = new RectOffset(20, 20, 16, 16) };
        }

        private static GUIStyle Label(int size, Color color, bool bold = false)
            => new GUIStyle(EditorStyles.label)
            {
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = color }
            };
    }
}
