using UnityEngine;
using UnityEngine.UI;

public static class MapEditorRenderUtil
{
    public static float CanvasScale(Canvas canvas)
    {
        return canvas != null ? canvas.scaleFactor : 1f;
    }

    public static int PixelSize(float rectSize, Canvas canvas)
    {
        return Mathf.Max(16, Mathf.RoundToInt(rectSize * CanvasScale(canvas)));
    }

    // Build (or rebuild) a render texture that matches the UI widget's on-screen pixels.
    // The old map editor stretched a fixed 800x800 / 162x555 texture, which turned square
    // tiles into rectangles and made the sidebar look grainy at any other resolution.
    public static bool EnsureRenderTexture(Camera camera, RawImage image, ref RenderTexture rt, int width, int height)
    {
        if (camera == null)
            return false;

        width = Mathf.Max(16, width);
        height = Mathf.Max(16, height);
        if (rt != null && rt.IsCreated() && rt.width == width && rt.height == height)
        {
            if (camera.targetTexture != rt)
                camera.targetTexture = rt;
            if (image != null && image.texture != rt)
                image.texture = rt;
            return false;
        }

        var next = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
        {
            name = camera.name + "RT",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1
        };
        next.Create();

        camera.targetTexture = next;
        if (image != null)
            image.texture = next;

        if (rt != null)
        {
            if (camera.targetTexture == rt)
                camera.targetTexture = next;
            rt.Release();
            Object.Destroy(rt);
        }

        rt = next;
        return true;
    }

    public static void Release(ref RenderTexture rt, Camera camera, RawImage image)
    {
        if (image != null && image.texture == rt)
            image.texture = null;
        if (camera != null && camera.targetTexture == rt)
            camera.targetTexture = null;
        if (rt != null)
        {
            rt.Release();
            Object.Destroy(rt);
            rt = null;
        }
    }

    public static void TileCheckerboard(RawImage image, float cellPixels)
    {
        if (image == null || cellPixels <= 0f)
            return;
        var rect = ((RectTransform)image.transform).rect;
        image.uvRect = new Rect(0f, 0f, rect.width / cellPixels, rect.height / cellPixels);
    }
}
