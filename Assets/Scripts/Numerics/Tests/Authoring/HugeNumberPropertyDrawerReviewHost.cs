using HugeNumberSystem.Numerics.Authoring;

using UnityEditor;
using UnityEngine;

namespace HugeNumberSystem.Numerics.Authoring.Tests
{
    public sealed class HugeNumberPropertyDrawerReviewHost : ScriptableObject
    {
        [SerializeField] private SerializedHugeNumber _value = new SerializedHugeNumber(125d, 4L);
    }

    public static class HugeNumberPropertyDrawerReviewMenu
    {
        [MenuItem("Huge Number System/Tests/Open Property Drawer Review")]
        private static void Open()
        {
            var host = ScriptableObject.CreateInstance<HugeNumberPropertyDrawerReviewHost>();
            host.name = "Huge Number Property Drawer Review";
            host.hideFlags = HideFlags.DontSave;
            Selection.activeObject = host;
        }
    }
}
