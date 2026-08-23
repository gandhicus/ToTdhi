using System;
using System.Collections;
using System.Collections.Generic;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Items;
using TitanCore.Net.Packets.Client;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int slotIndex;

    public Item item = Item.Blank;

    public ItemInfo info;

    public IContainer owner;

    public Image colorImage;

    public Graphic backgroundGraphic;

    public ItemDisplay itemDisplay;

    public TooltipManager tooltipManager;

    public Color unequippableColor = Color.white;

    public Color soulboundColor = Color.white;

    //private Vector2 slotPosition;

    private int tooltipId = -1;

    private bool dragging = false;

    public RectTransform rectTransform;

    public ItemSwapper swapper;

    private DateTime lastClick;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        backgroundGraphic = GetComponent<Graphic>();
    }

    private void Start()
    {
        //slotPosition = rectTransform.anchoredPosition;
    }

    public void SetItem(Item newItem)
    {
        if (newItem.IsBlank && item.IsBlank) return;
        if (newItem == item) return;

        itemDisplay.SetItem(newItem);
        item = newItem;
        if (!item.IsBlank)
            info = item.GetInfo();
        else
            info = null;
        UpdateColoring();

        if (tooltipId >= 0)
        {
            HideTooltip();
            ShowTooltip();
        }
    }

    public void SetOwner(IContainer owner, int slotIndex)
    {
        if (this.owner != null)
        {
            this.owner.RemoveOnInventoryUpdated(OnOwnerInventoryUpdated);
        }

        this.owner = owner;
        this.slotIndex = slotIndex;
        owner.SetOnInventoryUpdated(OnOwnerInventoryUpdated);

        itemDisplay.SetPlaceholderType(owner.GetSlotType(slotIndex));
        SetItem(owner.GetItem(slotIndex));
    }

    public void OnOwnerInventoryUpdated()
    {
        SetItem(owner.GetItem(slotIndex));
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.RemoveOnInventoryUpdated(OnOwnerInventoryUpdated);
            owner = null;
        }
    }

    private void UpdateColoring()
    {
        if (colorImage == null || owner == null || !(owner.GetInfo() is TitanCore.Data.Entities.CharacterInfo charInfo)) return;

        if (item.IsBlank)
        {
            colorImage.gameObject.SetActive(false);
            return;
        }

        if (SkillTreeFunctions.IsEnabled && slotIndex == SkillTreeFunctions.Talisman_Slot && info.slotType == SlotType.Talisman)
        {
            colorImage.gameObject.SetActive(false);
            return;
        }

        if (item.soulbound)
        {
            colorImage.gameObject.SetActive(true);
            colorImage.color = soulboundColor;
            return;
        }
        else if (info.slotType == SlotType.Generic)
        {
            colorImage.gameObject.SetActive(false);
            return;
        }

        else if (info is EquipmentInfo talismanEquip && talismanEquip.slotType == SlotType.Talisman)
        {
            bool usable = charInfo.CanUseEquipment(talismanEquip);
            colorImage.gameObject.SetActive(!usable);
            if (!usable)
                colorImage.color = unequippableColor;
            return;
        }

        else if (charInfo.CanUseSloType(info.slotType))
        {
            colorImage.gameObject.SetActive(false);
            return;
        }

        colorImage.gameObject.SetActive(true);
        colorImage.color = unequippableColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (eventData.pointerId != -1) return;
#endif
        if (item.IsBlank) return;
        if (!swapper.DragStarted(item, this)) return;

        dragging = true;
        itemDisplay.SetHidden(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (eventData.pointerId != -1) return;
#endif
        if (!dragging) return;
        HideTooltip();
        swapper.OnDrag(new Vector2((int)eventData.position.x, (int)eventData.position.y), this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (eventData.pointerId != -1) return;
#endif
        if (!dragging) return;
        dragging = false;
        itemDisplay.SetHidden(false);

        swapper.DragEnded(eventData.position, this);
    }

    private SlotType GetSlotType()
    {
        return owner.GetSlotType(slotIndex);
    }

    private bool CanSwapItemInto(Slot otherSlot)
    {
        if (item.IsBlank) return true;
        if (SkillTreeFunctions.IsEnabled && otherSlot.slotIndex == SkillTreeFunctions.Talisman_Slot)
        {
            var info = item.GetInfo();
            return info != null && info.slotType == SlotType.Talisman;
        }
        if (otherSlot.owner.GetInfo() is TitanCore.Data.Entities.CharacterInfo charInfo && otherSlot.slotIndex < 4)
            return item.CanSwapInto(otherSlot.GetSlotType(), charInfo, otherSlot.slotIndex);
        return item.CanSwapInto(otherSlot.GetSlotType());
    }

    public void Swap(Slot otherSlot)
    {
        if (item.IsBlank && otherSlot.item.IsBlank) return;
        if (!CanSwapItemInto(otherSlot) || !otherSlot.CanSwapItemInto(this)) return;

        owner.GetWorld().gameManager.client.SendAsync(new TnSwap(owner.GetGameId(), (uint)slotIndex, otherSlot.owner.GetGameId(), (uint)otherSlot.slotIndex));

        var itemTemp = item;
        SetItem(otherSlot.item);
        otherSlot.SetItem(itemTemp);

        otherSlot.owner.SetItem(otherSlot.slotIndex, otherSlot.item);
        owner.SetItem(slotIndex, item);
    }

    private void OnDisable()
    {
        HideTooltip();

        if (dragging)
        {
            dragging = false;
            itemDisplay.SetHidden(false);

            swapper.DragEnded(new Vector2(-9999, -9999), this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
#if UNITY_STANDALONE
        ShowTooltip();
#endif
    }

    public void OnPointerExit(PointerEventData eventData)
    {
#if UNITY_STANDALONE
        HideTooltip();
#endif
    }

    private void ShowTooltip()
    {
        if (item.IsBlank) return;
        if (tooltipManager.IsCurrentTooltip(tooltipId)) return;
        if (tooltipId >= 0) HideTooltip();
        tooltipId = tooltipManager.ShowTooltip(item, true);
    }

    private void HideTooltip()
    {
        if (tooltipId < 0) return;
        tooltipManager.HideTooltip(tooltipId);
        tooltipId = -1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
#if UNITY_IOS || UNITY_ANDROID
        ShowTooltip();
#endif

        if ((DateTime.Now - lastClick).TotalSeconds < 0.5f)
        {
            lastClick = default;
            Activate();
        }
        else
            lastClick = DateTime.Now;
    }

    public void Activate()
    {
        if (item.IsBlank) return;
        var world = owner.GetWorld();
        if (owner.GetGameId() != world.player.gameId)
        {
            for (int i = 4; i < 12; i++)
            {
                if (world.player.GetItem(i).IsBlank)
                {
                    Swap(world.gameManager.ui.playerSlots[i]);
                    return;
                }
            }
        }

        var info = item.GetInfo();
        if (info.consumable)
        {
            UseItem();
        }
        else if (info is EquipmentInfo equip && owner.GetGameId() == world.player.gameId)
        {
            if (SkillTreeFunctions.IsEnabled && equip.slotType == SlotType.Talisman)
            {
                var destItem = world.player.GetItem(SkillTreeFunctions.Talisman_Slot);
                world.gameManager.client.SendAsync(new TnSwap(owner.GetGameId(), (uint)slotIndex, world.player.GetGameId(), (uint)SkillTreeFunctions.Talisman_Slot));
                owner.SetItem(slotIndex, destItem);
                world.player.SetItem(SkillTreeFunctions.Talisman_Slot, item);
                SetItem(destItem);
                return;
            }
            if (equip.slotType == SlotType.Accessory)
            {
                for (int i = 0; i < 4; i++)
                {
                    var slotType = world.player.GetSlotType(i);
                    if (slotType == equip.slotType && world.player.GetItem(i).IsBlank)
                    {
                        Swap(world.gameManager.ui.playerSlots[i]);
                        return;
                    }
                }
            }

            if (equip.slotType == SlotType.Accessory && (slotIndex == 7 || slotIndex == 11))
            {
                for (int i = 3; i >= 0; i--)
                {
                    var slotType = world.player.GetSlotType(i);
                    if (slotType == equip.slotType)
                    {
                        Swap(world.gameManager.ui.playerSlots[i]);
                        return;
                    }
                }
            }
            else
            {
                var charInfo = (TitanCore.Data.Entities.CharacterInfo)world.player.GetInfo();
                for (int i = 0; i < 4; i++)
                {
                    if (charInfo.CanEquipInSlot(equip.slotType, i))
                    {
                        Swap(world.gameManager.ui.playerSlots[i]);
                        return;
                    }
                }
            }
        }
    }

    private void UseItem()
    {
        var world = owner.GetWorld();
        world.gameManager.client.SendAsync(new TnUseItem(owner.GetGameId(), slotIndex, world.clientTickId, ((Vector2)world.player.Position).ToVec2()));
    }
}
