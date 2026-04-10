using UnityEditor;

[CustomEditor(typeof(ExcavationDirtPile))]
public class ExcavationDirtPileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
