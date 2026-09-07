using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapEditorGameViewFit
{
    static MapEditorGameViewFit()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        // Wait a frame so the Game view has its real size before we snap zoom.
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += FitIfMapEditorPlaying;
        };
    }

    static void FitIfMapEditorPlaying()
    {
        if (!EditorApplication.isPlaying)
            return;
        if (UnityEngine.Object.FindFirstObjectByType<MapEditor>() == null)
            return;
        Fit();
    }

    // Game view Scale is only a viewer zoom. Values like 1.2x crop the overlay UI
    // so the sidebar looks like a sliver. Snap back to the scale that shows the whole game.
    public static void Fit()
    {
        try
        {
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
                return;

            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows == null || windows.Length == 0)
                return;

            foreach (EditorWindow gameView in windows)
            {
                if (gameView == null)
                    continue;

                var snapZoom = gameViewType.GetMethod("SnapZoom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (snapZoom != null)
                {
                    snapZoom.Invoke(gameView, null);
                    gameView.Repaint();
                    continue;
                }

                var defaultScaleField = gameViewType.GetField("m_defaultScale", BindingFlags.Instance | BindingFlags.NonPublic);
                var areaField = gameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultScaleField == null || areaField == null)
                    continue;

                var area = areaField.GetValue(gameView);
                if (area == null)
                    continue;

                float defaultScale = (float)defaultScaleField.GetValue(gameView);
                if (defaultScale < 0.05f)
                    defaultScale = 1f;

                var scaleField = area.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
                if (scaleField == null)
                    continue;

                scaleField.SetValue(area, new Vector2(defaultScale, defaultScale));
                gameView.Repaint();
            }
        }
        catch (Exception)
        {
            // Unity's Game view internals are not a public API and can change between versions.
        }
    }
}
