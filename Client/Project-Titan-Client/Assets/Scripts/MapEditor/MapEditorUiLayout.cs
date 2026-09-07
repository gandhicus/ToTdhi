using UnityEngine;

public class MapEditorUiLayout
{
    // Original layout used 20% of the screen and looked empty. A fixed pixel width
    // turned into a sliver when the Game view was wide. Keep a modest fraction of the
    // canvas so the strip scales with maximize and stays wide enough for the tile grid.
    private const float SidebarMin = 0.86f;

    private RectTransform canvasRt;
    private RectTransform back;
    private RectTransform transparency;
    private RectTransform worldView;
    private RectTransform scrollView;
    private RectTransform dropdown;
    private RectTransform draw;
    private RectTransform erase;
    private RectTransform fill;
    private RectTransform circle;
    private RectTransform export;
    private RectTransform import;
    private RectTransform clear;

    public void Bind(Canvas canvas, RectTransform worldViewRect)
    {
        if (canvas == null)
            return;

        canvasRt = (RectTransform)canvas.transform;
        worldView = worldViewRect;
        back = Find(canvasRt, "Back");
        transparency = Find(canvasRt, "Transparency");
        scrollView = Find(canvasRt, "Scroll View");
        dropdown = Find(canvasRt, "Dropdown");
        draw = Find(canvasRt, "Tool - Draw");
        erase = Find(canvasRt, "Tool - Erase");
        fill = Find(canvasRt, "Tool - Fill");
        circle = Find(canvasRt, "Tool - Circle");
        export = Find(canvasRt, "Export");
        import = Find(canvasRt, "Import");
        clear = Find(canvasRt, "Clear");
    }

    public void Apply()
    {
        if (canvasRt == null)
            return;

        Stretch(worldView, 0f, 0f, SidebarMin, 1f);
        Stretch(transparency, 0f, 0f, SidebarMin, 1f);
        Stretch(back, SidebarMin, 0f, 1f, 1f);

        float left = SidebarMin + 0.01f;
        float mid = SidebarMin + (1f - SidebarMin) * 0.5f - 0.005f;
        float midRight = mid + 0.01f;
        float right = 0.99f;

        Stretch(draw, left, 0.95f, mid, 0.985f);
        Stretch(erase, midRight, 0.95f, right, 0.985f);
        Stretch(fill, left, 0.90f, mid, 0.935f);
        Stretch(circle, midRight, 0.90f, right, 0.935f);
        Stretch(export, left, 0.85f, mid, 0.885f);
        Stretch(import, midRight, 0.85f, right, 0.885f);
        Stretch(clear, left, 0.80f, right, 0.835f);
        Stretch(dropdown, left, 0.71f, right, 0.75f);
        Stretch(scrollView, left, 0.006f, right, 0.70f);
    }

    private static RectTransform Find(Transform parent, string name)
    {
        var child = parent.Find(name);
        return child as RectTransform;
    }

    private static void Stretch(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }
}
