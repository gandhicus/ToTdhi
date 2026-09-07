using Assets.Scripts.MapEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Items;
using TitanCore.Data.Map;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum MapEditorObjectType
{
    Object,
    Tile,
    Region
}

public class ObjectLayout : MonoBehaviour, IPointerDownHandler
{
    public World world;

    public Camera objectCamera;

    public GameObject spritePrefab;

    public GameObject meshPrefab;

    public GameObject regionPrefab;

    public RectTransform content;

    public RectTransform selection;

    public MapEditorObject selectedObject;

    public TMP_Dropdown dropdown;

    public MapEditorObjectType selectedType = MapEditorObjectType.Object;

    private MapEditorObject[] objs;

    private MapEditorObject[] tiles;

    private MapEditorRegion[] regions;

    private int selectedIndex;

    private Canvas canvas;

    private Image selectionImage;

    private RawImage paletteImage;

    private RenderTexture paletteRT;

    private const float PaletteColumns = 3f;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        paletteImage = GetComponent<RawImage>();
        StretchPaletteToViewport();
        Canvas.ForceUpdateCanvases();
        LayoutAllObjects();

        // Keep the white square on the palette image itself so it can track the tile
        // the object camera is actually showing, instead of a mismatched Content grid.
        if (selection != null && selection.parent != transform)
        {
            selection.SetParent((RectTransform)transform, false);
            selection.anchorMin = Vector2.zero;
            selection.anchorMax = Vector2.zero;
            selection.pivot = Vector2.zero;
            selection.localScale = Vector3.one;
            selection.localRotation = Quaternion.identity;
        }
        selectionImage = selection != null ? selection.GetComponent<Image>() : null;
        if (selectionImage != null)
            selectionImage.raycastTarget = false;

        dropdown.ClearOptions();
        foreach (var value in (MapEditorObjectType[])Enum.GetValues(typeof(MapEditorObjectType)))
            dropdown.options.Add(new TMP_Dropdown.OptionData(value.ToString() + 's'));

        SetSelectionType(MapEditorObjectType.Object);
        Select(0);
        FitPaletteView();
        SyncCameraToScroll();
        UpdateSelectionHighlight();
    }

    public void LayoutAllObjects()
    {
        LayoutObjects();
        LayoutTiles();
        LayoutRegions();
    }

    private void LayoutObjects()
    {
        var objectInfos = GameData.objects.Values.Where(_ => !(_ is TileInfo) && !(_ is ItemInfo) && !(_ is ProjectileInfo) && !_.serverOnly).ToArray();

        int index = 0;
        objs = new MapEditorObject[objectInfos.Length];
        foreach (var objInfo in objectInfos)
        {
            var obj = CreateObject(objInfo, true);
            obj.SetPosition(new Vector3(index % 3, -index / 3, -3));
            objs[index] = obj;
            obj.gameObject.SetActive(false);
            index++;
        }
    }

    private void LayoutTiles()
    {
        var objectInfos = GameData.objects.Values.Where(_ => _ is TileInfo && !_.serverOnly).ToArray();

        int index = 0;
        tiles = new MapEditorObject[objectInfos.Length];
        foreach (var objInfo in objectInfos)
        {
            var obj = CreateObject(objInfo, true);
            obj.SetPosition(new Vector2(index % 3, -index / 3));
            tiles[index] = obj;
            obj.gameObject.SetActive(false);
            index++;
        }
    }

    private void LayoutRegions()
    {
        var regionTypes = (Region[])Enum.GetValues(typeof(Region));

        int index = 0;
        regions = new MapEditorRegion[regionTypes.Length];
        foreach (var regionType in regionTypes)
        {
            var obj = CreateRegion(regionType);
            obj.SetPosition(new Vector2(index % 3, -index / 3));
            regions[index] = obj;
            obj.gameObject.SetActive(false);
            index++;
        }
    }

    public void DropdownChanged(int index)
    {
        SetSelectionType((MapEditorObjectType)index);
        Select(0);
        ScrollToIndex(0);
    }

    public void SetSelectionType(MapEditorObjectType type)
    {
        foreach (var oldObj in ObjsForType(selectedType))
            oldObj.gameObject.SetActive(false);

        selectedType = type;
        var objs = ObjsForType(type);
        foreach (var obj in objs)
            obj.gameObject.SetActive(true);

        UpdateContentHeight();
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
    }

    private MapEditorObject[] ObjsForType(MapEditorObjectType type)
    {
        switch (type)
        {
            case MapEditorObjectType.Tile:
                return tiles;
            case MapEditorObjectType.Region:
                return regions;
            default:
                return objs;
        }
    }

    public MapEditorObject CreateObject(GameObjectInfo info, bool forDisplay)
    {
        MapEditorObject obj;
        switch (info.Type)
        {
            case GameObjectType.Wall:
            case GameObjectType.Object3d:
                obj = Instantiate(meshPrefab).GetComponent<MapEditorMesh>();
                break;
            default:
                obj = Instantiate(spritePrefab).GetComponent<MapEditorSprite>();
                break;
        }
        obj.LoadInfo(info, forDisplay);
        return obj;
    }

    public MapEditorRegion CreateRegion(Region region)
    {
        var obj = Instantiate(regionPrefab).GetComponent<MapEditorRegion>();
        obj.SetRegion(region);
        return obj;
    }

    private void LateUpdate()
    {
        StretchPaletteToViewport();
        FitPaletteView();
        UpdateContentHeight();
        SyncCameraToScroll();
        UpdateSelectionHighlight();
    }

    private void StretchPaletteToViewport()
    {
        var rectTransform = (RectTransform)transform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    // Size the palette camera to the panel and show 3 square columns. A fixed 162x555
    // texture stretched into this tall sidebar is what made the tiles look like rectangles.
    private void FitPaletteView()
    {
        if (objectCamera == null)
            return;

        var rect = ((RectTransform)transform).rect;
        int pixelWidth = MapEditorRenderUtil.PixelSize(rect.width, canvas);
        int pixelHeight = MapEditorRenderUtil.PixelSize(rect.height, canvas);
        MapEditorRenderUtil.EnsureRenderTexture(objectCamera, paletteImage, ref paletteRT, pixelWidth, pixelHeight);

        float aspect = objectCamera.aspect;
        if (aspect <= 0.01f)
            return;
        objectCamera.orthographicSize = (PaletteColumns / aspect) * 0.5f;
    }

    // Palette tiles are world objects shown in a texture that now matches this panel.
    // Content height uses the on-screen size of one square cell so scrolling stays lined up.
    private void UpdateContentHeight()
    {
        var paletteObjs = ObjsForType(selectedType);
        if (paletteObjs == null)
            return;

        int rows = Mathf.Max(1, (paletteObjs.Length + 2) / 3);
        float pixelsPerWorld = GetPalettePixelsPerWorldUnit();
        float height = rows * pixelsPerWorld;
        if (!Mathf.Approximately(content.sizeDelta.y, height))
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
    }

    private void SyncCameraToScroll()
    {
        float pixelsPerWorld = GetPalettePixelsPerWorldUnit();
        if (pixelsPerWorld <= 0.01f)
            return;

        float scrolledWorld = content.anchoredPosition.y / pixelsPerWorld;
        float ortho = objectCamera.orthographicSize;
        objectCamera.transform.position = new Vector3(PaletteColumns * 0.5f, -ortho - scrolledWorld, -5f);
    }

    private float GetPalettePixelsPerWorldUnit()
    {
        float viewWidth = ((RectTransform)transform).rect.width;
        if (viewWidth <= 0.01f)
            return 1f;
        return viewWidth / PaletteColumns;
    }

    private Camera GetCanvasEventCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return canvas.worldCamera;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != 0) return;
        if (!TryGetIndexAtScreen(eventData.position, out int index))
            return;
        Select(index);
    }

    private bool TryGetIndexAtScreen(Vector2 screenPos, out int index)
    {
        index = -1;
        var rectTransform = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, GetCanvasEventCamera(), out var local))
            return false;

        var rect = rectTransform.rect;
        if (rect.width <= 0.01f || rect.height <= 0.01f)
            return false;

        float u = (local.x - rect.xMin) / rect.width;
        float v = (local.y - rect.yMin) / rect.height;
        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return false;

        var world = objectCamera.ViewportToWorldPoint(new Vector3(u, v, 0f));
        int col = Mathf.FloorToInt(world.x);
        int row = Mathf.FloorToInt(-world.y);
        if (col < 0 || col > 2 || row < 0)
            return false;

        index = row * 3 + col;
        var paletteObjs = ObjsForType(selectedType);
        return index >= 0 && index < paletteObjs.Length;
    }

    public void Select(int index)
    {
        switch (selectedType)
        {
            case MapEditorObjectType.Tile:
                SelectTile(ref index);
                break;
            case MapEditorObjectType.Region:
                SelectRegion(ref index);
                break;
            default:
                SelectObj(ref index);
                break;
        }

        selectedIndex = index;
        UpdateSelectionHighlight();
    }

    public bool SelectFromWorld(MapEditorObject worldObj, MapEditorObjectType type)
    {
        if (worldObj == null)
            return false;

        if (type != selectedType)
        {
            if (dropdown != null)
                dropdown.SetValueWithoutNotify((int)type);
            SetSelectionType(type);
        }

        var paletteObjs = ObjsForType(type);
        int index = -1;
        if (type == MapEditorObjectType.Region)
        {
            var region = ((MapEditorRegion)worldObj).region;
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].region == region)
                {
                    index = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < paletteObjs.Length; i++)
            {
                if (paletteObjs[i].info.id == worldObj.info.id)
                {
                    index = i;
                    break;
                }
            }
        }

        if (index < 0)
            return false;

        Select(index);
        ScrollToIndex(index);
        return true;
    }

    public void ScrollToIndex(int index)
    {
        int row = index / 3;
        float pixelsPerWorld = GetPalettePixelsPerWorldUnit();
        float viewportHeight = ((RectTransform)transform).rect.height;
        float targetY = row * pixelsPerWorld - viewportHeight * 0.5f + pixelsPerWorld * 0.5f;
        float maxY = Mathf.Max(0f, content.sizeDelta.y - viewportHeight);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(targetY, 0f, maxY));
        SyncCameraToScroll();
        UpdateSelectionHighlight();
    }

    private void UpdateSelectionHighlight()
    {
        if (selection == null)
            return;

        int col = selectedIndex % 3;
        int row = selectedIndex / 3;

        // Display sprites sit at (col, -row) with a top-left pivot, so the cell is
        // x [col, col+1] and y [-row-1, -row]. Project that through the object camera
        // so the square stays on the tile as the list scrolls.
        var bottomLeft = objectCamera.WorldToViewportPoint(new Vector3(col, -row - 1f, 0f));
        var topRight = objectCamera.WorldToViewportPoint(new Vector3(col + 1f, -row, 0f));

        bool visible = topRight.x > 0f && bottomLeft.x < 1f && topRight.y > 0f && bottomLeft.y < 1f;
        selection.gameObject.SetActive(visible);
        if (!visible)
            return;

        float xMin = Mathf.Clamp01(bottomLeft.x);
        float xMax = Mathf.Clamp01(topRight.x);
        float yMin = Mathf.Clamp01(bottomLeft.y);
        float yMax = Mathf.Clamp01(topRight.y);

        var rect = ((RectTransform)transform).rect;
        selection.anchoredPosition = new Vector2(xMin * rect.width, yMin * rect.height);
        selection.sizeDelta = new Vector2(Mathf.Max(0f, (xMax - xMin) * rect.width), Mathf.Max(0f, (yMax - yMin) * rect.height));
    }

    private void SelectObj(ref int index)
    {
        index = Mathf.Min(objs.Length - 1, index);
        var obj = objs[index];
        selectedObject = obj;
    }

    private void SelectTile(ref int index)
    {
        index = Mathf.Min(tiles.Length - 1, index);
        var obj = tiles[index];
        selectedObject = obj;
    }

    private void SelectRegion(ref int index)
    {
        index = Mathf.Min(regions.Length - 1, index);
        var obj = regions[index];
        selectedObject = obj;
    }

    private void OnDestroy()
    {
        MapEditorRenderUtil.Release(ref paletteRT, objectCamera, paletteImage);
    }
}
