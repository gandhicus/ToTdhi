using TitanCore.Data;
using TitanCore.Net.Packets.Models;
using UnityEngine;

public class Waypoint : SpriteWorldObject
{
    private const float VisualYOffset = 0.55f;
    private const float VisualHover = 0.15f;
    private const float MapIndicatorSize = 0.2f;

    private Transform visualTransform;

    public override GameObjectType ObjectType => GameObjectType.Waypoint;

    protected override bool HasShadow => false;

    public string waypointName { get; private set; } = "";

    protected override void Awake()
    {
        base.Awake();
        SetupVisualTransform();
    }

    public override void Enable()
    {
        base.Enable();

        SetHover(VisualHover);
        ShowGroundLabel(null);
    }

    public override void LoadObjectInfo(GameObjectInfo info)
    {
        base.LoadObjectInfo(info);

        name = info.name;

        indicator.spriteRenderer.sprite = TextureManager.GetDisplaySprite(info);
        indicator.spriteRenderer.color = Color.white;
        indicator.sizeAdjustment = MapIndicatorSize;
        indicator.UpdateSize();
    }

    private void SetupVisualTransform()
    {
        if (visualTransform != null) return;

        visualTransform = transform.Find("Visual");
        if (visualTransform != null)
        {
            visualTransform.localPosition = new Vector3(0f, VisualYOffset, 0f);
            if (spriteRenderer == null)
                spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
            return;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null) return;

        var visualGo = new GameObject("Visual");
        visualTransform = visualGo.transform;
        visualTransform.SetParent(transform, false);
        visualTransform.localPosition = new Vector3(0f, VisualYOffset, 0f);

        var visualRenderer = visualGo.AddComponent<SpriteRenderer>();
        CopySpriteRenderer(spriteRenderer, visualRenderer);
        Destroy(spriteRenderer);
        spriteRenderer = visualRenderer;
    }

    private static void CopySpriteRenderer(SpriteRenderer from, SpriteRenderer to)
    {
        to.sprite = from.sprite;
        to.color = from.color;
        to.flipX = from.flipX;
        to.flipY = from.flipY;
        to.sortingLayerID = from.sortingLayerID;
        to.sortingOrder = from.sortingOrder;
        to.spriteSortPoint = from.spriteSortPoint;
    }

    protected override void ProcessStat(NetStat stat, bool first)
    {
        base.ProcessStat(stat, first);

        if (stat.type == ObjectStatType.Name)
            waypointName = (string)stat.value;
    }
}
