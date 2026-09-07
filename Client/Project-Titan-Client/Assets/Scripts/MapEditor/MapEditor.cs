using Assets.Scripts.MapEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Entities;
using TitanCore.Files;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.NET.Geometry;
using static TitanCore.Files.MapElementFile;

public class MapEditor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Camera worldCamera;

    public ObjectLayout objLayout;

    public MapEditorObject[,] objects;

    public MapEditorObject[,] tiles;

    public MapEditorRegion[,] regions;

    public MapEditorTool[] toolsArray;

    public TileRotation tileRotation;

    public MapEditorTool currentTool;

    private Dictionary<MapEditorToolType, MapEditorTool> tools;

    private int width = 1000;

    private int height = 1000;

    private RectTransform worldViewRect;

    private Canvas worldViewCanvas;

    private RawImage worldViewImage;

    private RawImage transparencyImage;

    private RenderTexture worldRT;

    private Camera displayCamera;

    private readonly MapEditorUiLayout uiLayout = new MapEditorUiLayout();

    // Painting records one snapshot per cell the first time a stroke touches it, so Ctrl+Z
    // can put the whole brush / fill / circle back the way it was.
    private const int MaxUndoStrokes = 50;

    private readonly List<CellEdit> currentStroke = new List<CellEdit>();

    private readonly HashSet<long> currentStrokeKeys = new HashSet<long>();

    private readonly List<List<CellEdit>> undoStack = new List<List<CellEdit>>();

    private readonly List<List<CellEdit>> redoStack = new List<List<CellEdit>>();

    private bool strokeOpen;

    private bool suppressHistory;

    private Int2 lastToolPosition;

    private struct CellEdit
    {
        public int x;
        public int y;
        public MapEditorObjectType type;
        public bool hadObject;
        public ushort prevId;
        public Region prevRegion;
        public TileRotation prevRotation;
    }

    private void Start()
    {
        worldViewRect = (RectTransform)transform;
        worldViewCanvas = GetComponentInParent<Canvas>();
        worldViewImage = GetComponent<RawImage>();
        var transparency = transform.parent != null ? transform.parent.Find("Transparency") : null;
        if (transparency != null)
            transparencyImage = transparency.GetComponent<RawImage>();

        Canvas.ForceUpdateCanvases();
        FitCanvasToScreen();
        uiLayout.Bind(worldViewCanvas, worldViewRect);
        uiLayout.Apply();
        EnsureDisplayCamera();
        FitWorldView();

        objects = new MapEditorObject[width, height];
        tiles = new MapEditorObject[width, height];
        regions = new MapEditorRegion[width, height];

        tools = toolsArray.ToDictionary(_ => _.Type);
        SetTool(MapEditorToolType.Draw);
    }

    private MapEditorObject[,] GetObjs(MapEditorObjectType type)
    {
        switch (type)
        {
            case MapEditorObjectType.Tile:
                return tiles;
            case MapEditorObjectType.Region:
                return regions;
            default:
                return objects;
        }
    }

    public void Export()
    {
#if UNITY_EDITOR
        // World server loads maps from Map/Files next to the published exe, copied from this folder on StartServer.
        var filePath = EditorUtility.SaveFilePanel("Export Map", GetDefaultMapFilesDirectory(), "nexus", "mef");
        if (string.IsNullOrWhiteSpace(filePath)) return;
        var map = ExportMap();
        using (var stream = new MemoryStream())
        {
            map.Write(stream);
            File.WriteAllBytes(filePath, stream.ToArray());
        }
#endif
    }

#if UNITY_EDITOR
    // Assets -> Project-Titan-Client -> Client -> repo root -> server map folder.
    // Exporting here is enough: StartServer.bat copies this file into Builds/LocalServer on publish.
    private static string GetDefaultMapFilesDirectory()
    {
        var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Server", "Project-Titan", "World", "Map", "Files"));
        return Directory.Exists(dir) ? dir : "";
    }
#endif

    private MapElementFile ExportMap()
    {
        var bl = new Int2(width, height);
        var tr = new Int2(0, 0);

        for (int y = 0; y < height; y++) // get bounds
            for (int x = 0; x < width; x++)
            {
                var tile = tiles[x, y];
                var obj = objects[x, y];
                var region = regions[x, y];
                if (tile == null && obj == null && region == null) continue;
                if (x < bl.x)
                    bl.x = x;
                if (x > tr.x)
                    tr.x = x;

                if (y < bl.y)
                    bl.y = y;
                if (y > tr.y)
                    tr.y = y;
            }

        var file = new MapElementFile();
        file.width = tr.x - bl.x + 1;
        file.height = tr.y - bl.y + 1;

        var tileElements = new MapTileElement[file.width, file.height];
        for (int y = 0; y < file.height; y++) // get bounds
            for (int x = 0; x < file.width; x++)
            {
                var tile = tiles[bl.x + x, bl.y + y];
                var obj = objects[bl.x + x, bl.y + y];
                tileElements[x, y] = new MapTileElement()
                {
                    tileType = tile == null ? (ushort)0 : tile.info.id,
                    objectType = obj == null ? (ushort)0 : obj.info.id
                };
            }
        file.tiles = tileElements;
        file.entities = new MapEntityElement[0];

        var regionList = new List<MapRegionElement>();
        for (int y = 0; y < file.height; y++)
            for (int x = 0; x < file.width; x++)
            {
                var region = regions[bl.x + x, bl.y + y];
                if (region == null) continue;
                regionList.Add(new MapRegionElement()
                {
                    x = (uint)x,
                    y = (uint)y,
                    regionType = region.region
                });
            }
        file.regions = regionList.ToArray();

        return file;
    }

    public void Import()
    {
#if UNITY_EDITOR
        var filePath = EditorUtility.OpenFilePanel("Import Map", GetDefaultMapFilesDirectory(), "mef");
        if (string.IsNullOrWhiteSpace(filePath)) return;
        var file = MapElementFile.ReadFrom(filePath);
        suppressHistory = true;
        Clear();
        ImportMap(file);
        suppressHistory = false;
        ClearHistory();
#endif
    }

    private void ImportMap(MapElementFile file)
    {
        width = Math.Max(1000, file.width);
        height = Math.Max(1000, file.height);

        var bl = new Int2(width / 2 - file.width / 2, height / 2 - file.height / 2);
        for (int y = 0; y < file.height; y++)
            for (int x = 0; x < file.width; x++)
            {
                var tile = file.tiles[x, y];
                if (tile.tileType != 0)
                    CreateObject(bl.x + x, bl.y + y, GameData.objects[tile.tileType], MapEditorObjectType.Tile);
                if (tile.objectType != 0)
                    CreateObject(bl.x + x, bl.y + y, GameData.objects[tile.objectType], MapEditorObjectType.Object);
            }

        foreach (var region in file.regions)
        {
            CreateRegion(bl.x + (int)region.x, bl.y + (int)region.y, region.regionType, MapEditorObjectType.Region);
        }

        worldCamera.transform.position = new Vector3(width / 2, height / 2, worldCamera.transform.position.z);
    }

    public void Clear()
    {
        for (int y = 0; y < width; y++) // get bounds
            for (int x = 0; x < height; x++)
            {
                var tile = tiles[x, y];
                if (tile != null)
                    Destroy(tile.gameObject);

                var obj = objects[x, y];
                if (obj != null)
                    Destroy(obj.gameObject);

                var region = regions[x, y];
                if (region != null)
                    Destroy(region.gameObject);
            }

        width = 1000;
        height = 1000;
        objects = new MapEditorObject[width, height];
        tiles = new MapEditorObject[width, height];
        regions = new MapEditorRegion[width, height];
        ClearHistory();
    }

    public void SetDraw()
    {
        SetTool(MapEditorToolType.Draw);
    }

    public void SetErase()
    {
        SetTool(MapEditorToolType.Erase);
    }

    public void SetFill()
    {
        SetTool(MapEditorToolType.Fill);
    }

    public void SetCircle()
    {
        SetTool(MapEditorToolType.Circle);
    }

    public void SetTool(MapEditorToolType type)
    {
        if (currentTool != null && currentTool.Type == type) return;
        if (!tools.TryGetValue(type, out var newTool))
            return;
        currentTool?.SetNormal();
        currentTool = newTool;
        currentTool.SetSelected();
    }

    // Overlay UI shows a stretched copy of the world camera's render texture. Feeding
    // Input.mousePosition straight into ScreenToWorldPoint treats those pixels as if they
    // were on the texture itself, which only lines up at one resolution. Map through the
    // RawImage's UVs first so the tile under the cursor is the tile that gets painted.
    public bool TryScreenToWorld(Vector3 screenPos, out Vector3 worldPos, bool requireInside = true)
    {
        worldPos = default;
        if (worldViewRect == null || worldCamera == null)
            return false;

        var eventCam = GetCanvasEventCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(worldViewRect, screenPos, eventCam, out var local))
            return false;

        var rect = worldViewRect.rect;
        if (rect.width <= 0.01f || rect.height <= 0.01f)
            return false;

        float u = (local.x - rect.xMin) / rect.width;
        float v = (local.y - rect.yMin) / rect.height;
        if (requireInside && (u < 0f || u > 1f || v < 0f || v > 1f))
            return false;

        worldPos = worldCamera.ViewportToWorldPoint(new Vector3(u, v, worldCamera.nearClipPlane));
        return true;
    }

    public bool IsPointerOverWorldView()
    {
        if (worldViewRect == null)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(worldViewRect, Input.mousePosition, GetCanvasEventCamera());
    }

    private Camera GetCanvasEventCamera()
    {
        if (worldViewCanvas == null || worldViewCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return worldViewCanvas.worldCamera;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != 0) return;

        // Alt+click picks the tile under the cursor instead of painting.
        if (IsAltHeld())
        {
            PickFromCursor();
            return;
        }

        BeginStroke();
        var pos = GetToolPosition(eventData.position);
        currentTool?.StartTool(pos.x, pos.y);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != 0) return;
        if (!strokeOpen) return;
        var pos = GetToolPosition(eventData.position);
        currentTool?.DoTool(pos.x, pos.y);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != 0) return;
        if (!strokeOpen) return;
        var pos = GetToolPosition(eventData.position);
        currentTool?.EndTool(pos.x, pos.y);
        EndStroke();
    }

    private Int2 GetToolPosition(Vector2 screenPos)
    {
        if (TryScreenToWorld(screenPos, out var world))
        {
            var objs = GetObjs(objLayout.selectedType);
            var intPos = new Int2(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
            intPos.x = Mathf.Min(Mathf.Max(intPos.x, 0), objs.GetLength(0) - 1);
            intPos.y = Mathf.Min(Mathf.Max(intPos.y, 0), objs.GetLength(1) - 1);
            lastToolPosition = intPos;
        }
        return lastToolPosition;
    }

    public void Fill(int x, int y, MapEditorObjectType sampleFrom, bool clear)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;

        var sampleGroup = GetObjs(sampleFrom);

        ushort toFillType = sampleGroup[x, y]?.Id ?? 0;

        if (toFillType == objLayout.selectedObject.Id) return;

        var filled = new HashSet<Int2>();
        var toProcess = new Queue<Int2>();
        toProcess.Enqueue(new Int2(x, y));

        while (toProcess.Count > 0)
        {
            var point = toProcess.Dequeue();
            if (point.x < 0 || point.y < 0 || point.x >= width || point.y >= height || filled.Contains(point)) continue;

            var objId = sampleGroup[point.x, point.y]?.Id ?? 0;
            if (objId == toFillType)
            {
                if (clear)
                    EraseObject(point.x, point.y);
                else
                    CreateObject(point.x, point.y);

                filled.Add(point);

                toProcess.Enqueue(new Int2(point.x + 1, point.y));
                toProcess.Enqueue(new Int2(point.x - 1, point.y));
                toProcess.Enqueue(new Int2(point.x, point.y + 1));
                toProcess.Enqueue(new Int2(point.x, point.y - 1));
            }
        }
    }

    public void CreateObject(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        RecordCell(x, y, objLayout.selectedType);
        if (objLayout.selectedType == MapEditorObjectType.Region)
            CreateRegion(x, y, ((MapEditorRegion)objLayout.selectedObject).region, MapEditorObjectType.Region);
        else
            CreateObject(x, y, objLayout.selectedObject.info, objLayout.selectedType);
    }

    private void CreateObject(int x, int y, GameObjectInfo info, MapEditorObjectType type)
    {
        DestroyObject(x, y, type);

        var obj = objLayout.CreateObject(info, false);

        SetupObject(x, y, obj, type);
    }

    private void CreateRegion(int x, int y, Region region, MapEditorObjectType type)
    {
        DestroyObject(x, y, type);

        var obj = objLayout.CreateRegion(region);

        SetupObject(x, y, obj, type);
    }

    private void DestroyObject(int x, int y, MapEditorObjectType type)
    {
        var objs = GetObjs(type);
        var currentObj = objs[x, y];
        if (currentObj != null)
        {
            Destroy(currentObj.gameObject);
            objs[x, y] = null;
        }
    }

    private void SetupObject(int x, int y, MapEditorObject obj, MapEditorObjectType type)
    {
        var offset = new Vector2(0, 1);
        if (type == MapEditorObjectType.Tile)
        {
            obj.transform.localEulerAngles = new Vector3(0, 0, (int)tileRotation * 90);
            switch (tileRotation)
            {
                case TileRotation.Quarter:
                    offset = new Vector2(0, 0);
                    break;
                case TileRotation.Half:
                    offset = new Vector2(1, 0);
                    break;
                case TileRotation.ThreeQuarters:
                    offset = new Vector2(1, 1);
                    break;
            }
        }

        obj.SetPosition(new Vector3(x + offset.x, y + offset.y, DepthOfType(type)));
        obj.gameObject.layer = 0;
        for (int i = 0; i < obj.transform.childCount; i++)
            obj.transform.GetChild(i).gameObject.layer = 0;
        var objs = GetObjs(type);
        objs[x, y] = obj;
    }

    public void EraseObject(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        var objs = GetObjs(objLayout.selectedType);
        var currentObj = objs[x, y];
        if (currentObj == null) return;
        RecordCell(x, y, objLayout.selectedType);
        Destroy(currentObj.gameObject);
        objs[x, y] = null;
    }

    private int DepthOfType(MapEditorObjectType type)
    {
        switch (type)
        {
            case MapEditorObjectType.Tile:
                return -1;
            case MapEditorObjectType.Region:
                return -9;
            default:
                return -2;
        }
    }

    private void Update()
    {
        uiLayout.Apply();
        FitWorldView();
    }

    private void LateUpdate()
    {
        if (!IsCtrlHeld())
        {
            foreach (var tool in toolsArray)
            {
                if (Input.GetKeyDown(tool.selectKey))
                {
                    SetTool(tool.Type);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                tileRotation = (TileRotation)(((int)tileRotation + 1) % 4);
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            PickFromCursor();
        }

        if (IsCtrlHeld() && Input.GetKeyDown(KeyCode.Z))
        {
            if (IsShiftHeld())
                Redo();
            else
                Undo();
        }
        else if (IsCtrlHeld() && Input.GetKeyDown(KeyCode.Y))
        {
            Redo();
        }

        if (currentTool is CircleTool circleTool)
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKey(KeyCode.Equals))
            {
                circleTool.diameter++;
            }
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                circleTool.diameter = Mathf.Max(1, circleTool.diameter - 1);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                circleTool.clearMode = !circleTool.clearMode;
            }
        }

        if (currentTool is FillTool fillTool)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                fillTool.IncrementSampleType();
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                fillTool.clearMode = !fillTool.clearMode;
            }
        }
    }

    private static bool IsCtrlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private static bool IsShiftHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private static bool IsAltHeld()
    {
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    // Prefer the active palette layer so you can pick a ground tile even when an object
    // sits on the same cell. If that layer is empty, take whatever is visually on top.
    private void PickFromCursor()
    {
        if (!TryScreenToWorld(Input.mousePosition, out var world))
            return;

        int x = Mathf.FloorToInt(world.x);
        int y = Mathf.FloorToInt(world.y);
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;

        var preferredType = objLayout.selectedType;
        var preferred = GetObjs(preferredType)[x, y];
        if (TrySelectWorldObject(preferred, preferredType))
            return;

        if (TrySelectWorldObject(objects[x, y], MapEditorObjectType.Object))
            return;
        if (TrySelectWorldObject(tiles[x, y], MapEditorObjectType.Tile))
            return;
        TrySelectWorldObject(regions[x, y], MapEditorObjectType.Region);
    }

    private bool TrySelectWorldObject(MapEditorObject obj, MapEditorObjectType type)
    {
        if (obj == null)
            return false;
        if (!objLayout.SelectFromWorld(obj, type))
            return false;
        if (type == MapEditorObjectType.Tile)
            tileRotation = RotationFrom(obj);
        return true;
    }

    private static TileRotation RotationFrom(MapEditorObject obj)
    {
        int quarter = Mathf.RoundToInt(obj.transform.localEulerAngles.z / 90f) & 3;
        return (TileRotation)quarter;
    }

    private void BeginStroke()
    {
        if (suppressHistory)
            return;
        currentStroke.Clear();
        currentStrokeKeys.Clear();
        strokeOpen = true;
    }

    private void EndStroke()
    {
        if (!strokeOpen)
            return;
        strokeOpen = false;
        if (currentStroke.Count == 0)
            return;

        undoStack.Add(new List<CellEdit>(currentStroke));
        if (undoStack.Count > MaxUndoStrokes)
            undoStack.RemoveAt(0);
        redoStack.Clear();
        currentStroke.Clear();
        currentStrokeKeys.Clear();
    }

    private void RecordCell(int x, int y, MapEditorObjectType type)
    {
        if (suppressHistory || !strokeOpen)
            return;

        long key = ((long)(int)type << 40) | ((long)y << 20) | (uint)x;
        if (!currentStrokeKeys.Add(key))
            return;

        currentStroke.Add(CaptureCell(x, y, type));
    }

    private CellEdit CaptureCell(int x, int y, MapEditorObjectType type)
    {
        var current = GetObjs(type)[x, y];
        var edit = new CellEdit
        {
            x = x,
            y = y,
            type = type,
            hadObject = current != null
        };
        if (current == null)
            return edit;

        if (type == MapEditorObjectType.Region)
            edit.prevRegion = ((MapEditorRegion)current).region;
        else
            edit.prevId = current.info.id;

        if (type == MapEditorObjectType.Tile)
            edit.prevRotation = RotationFrom(current);

        return edit;
    }

    private void Undo()
    {
        if (strokeOpen)
            EndStroke();
        if (undoStack.Count == 0)
            return;

        var stroke = PopLast(undoStack);
        var redoStroke = new List<CellEdit>(stroke.Count);
        suppressHistory = true;
        for (int i = 0; i < stroke.Count; i++)
        {
            var edit = stroke[i];
            redoStroke.Add(CaptureCell(edit.x, edit.y, edit.type));
            RestoreCell(edit);
        }
        suppressHistory = false;
        redoStack.Add(redoStroke);
    }

    private void Redo()
    {
        if (strokeOpen)
            EndStroke();
        if (redoStack.Count == 0)
            return;

        var stroke = PopLast(redoStack);
        var undoStroke = new List<CellEdit>(stroke.Count);
        suppressHistory = true;
        for (int i = 0; i < stroke.Count; i++)
        {
            var edit = stroke[i];
            undoStroke.Add(CaptureCell(edit.x, edit.y, edit.type));
            RestoreCell(edit);
        }
        suppressHistory = false;
        undoStack.Add(undoStroke);
    }

    private void RestoreCell(CellEdit edit)
    {
        if (!edit.hadObject)
        {
            DestroyObject(edit.x, edit.y, edit.type);
            return;
        }

        if (edit.type == MapEditorObjectType.Region)
        {
            CreateRegion(edit.x, edit.y, edit.prevRegion, MapEditorObjectType.Region);
            return;
        }

        if (!GameData.objects.TryGetValue(edit.prevId, out var info))
            return;

        var savedRotation = tileRotation;
        tileRotation = edit.prevRotation;
        CreateObject(edit.x, edit.y, info, edit.type);
        tileRotation = savedRotation;
    }

    private void ClearHistory()
    {
        strokeOpen = false;
        currentStroke.Clear();
        currentStrokeKeys.Clear();
        undoStack.Clear();
        redoStack.Clear();
    }

    private void FitCanvasToScreen()
    {
        if (worldViewCanvas == null)
            return;

        var scaler = worldViewCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null)
            return;

        // Constant Pixel Size laid the UI out in raw pixels, so a zoomed Game view
        // cropped the top and right of the overlay. Scale with the screen so the
        // whole editor stays on camera.
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void FitWorldView()
    {
        if (worldViewRect == null || worldCamera == null)
            return;

        int pixelWidth = MapEditorRenderUtil.PixelSize(worldViewRect.rect.width, worldViewCanvas);
        int pixelHeight = MapEditorRenderUtil.PixelSize(worldViewRect.rect.height, worldViewCanvas);
        MapEditorRenderUtil.EnsureRenderTexture(worldCamera, worldViewImage, ref worldRT, pixelWidth, pixelHeight);
        MapEditorRenderUtil.TileCheckerboard(transparencyImage, 16f);
    }

    // World / palette cameras draw into textures, so Unity otherwise shows "No cameras
    // rendering" over the Game view. This dummy camera only clears the display behind the UI.
    private void EnsureDisplayCamera()
    {
        if (displayCamera != null)
            return;

        var go = new GameObject("MapEditorDisplayCamera");
        displayCamera = go.AddComponent<Camera>();
        displayCamera.clearFlags = CameraClearFlags.SolidColor;
        displayCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        displayCamera.cullingMask = 0;
        displayCamera.depth = -100;
        displayCamera.orthographic = true;
        displayCamera.allowHDR = false;
        displayCamera.allowMSAA = false;
    }

    private void OnDestroy()
    {
        MapEditorRenderUtil.Release(ref worldRT, worldCamera, worldViewImage);
        if (displayCamera != null)
            Destroy(displayCamera.gameObject);
    }

    private static List<CellEdit> PopLast(List<List<CellEdit>> stack)
    {
        int last = stack.Count - 1;
        var stroke = stack[last];
        stack.RemoveAt(last);
        return stroke;
    }

    // Stop Unity's editor undo from eating Ctrl+Z / Ctrl+Y while the map editor is playing.
    private void OnGUI()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown || !e.control)
            return;
        if (e.keyCode == KeyCode.Z || e.keyCode == KeyCode.Y)
            e.Use();
    }
}
