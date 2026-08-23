using Pc;
using System.Collections.Generic;
using TitanCore.Core;
using TitanCore.Net;
using TitanCore.Net.Packets.Client;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillTreeMenu : GameMenu
{
    public const float MenuWidth = 380f;
    public const float MenuHeight = 540f;

    [Header("Skill Tree layout — pause Play Mode and edit this component or its children")]
    public float iconSize = 72f;
    public float iconGap = 4f;
    public float rankFontSize = 24f;
    public float headerFontSize = 24f;

    public override MenuType MenuType => MenuType.SkillTree;

    private static TMP_FontAsset numberFont;

    private Transform contentRoot;
    private Image[] nodeIcons = new Image[SkillTreeFunctions.Node_Count];
    private TextMeshProUGUI[] rankLabels = new TextMeshProUGUI[SkillTreeFunctions.Node_Count];
    private TextMeshProUGUI costLabel;
    private TextMeshProUGUI subtitleLabel;
    private Slot socketSlot;
    private int hoverNode = -1;
    private int hideHoverFrames;
    private ItemTooltip nodeTooltip;
    private int tooltipKey = int.MinValue;
    private int[] pendingRanks = new int[SkillTreeFunctions.Node_Count];

    public Slot SocketSlot => socketSlot;

    public static SkillTreeMenu CreateRuntime(Transform parent, World world)
    {
        if (numberFont == null)
            numberFont = Resources.Load<TMP_FontAsset>("Fonts/no_continue SDF-Drop");

        var prefab = FindLevelUpPrefab(parent);
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = "SkillTreeMenu";
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one * LevelUpMenu.MenuScale;
#if UNITY_IOS || UNITY_ANDROID
            rect.anchoredPosition = Vector2.zero;
#else
            rect.anchoredPosition = new Vector2(Screen.width * 0.78f, 0f);
#endif
            rect.sizeDelta = new Vector2(MenuWidth, MenuHeight);
        }
        else
        {
            go = new GameObject("SkillTreeMenu", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }

        var levelUp = go.GetComponent<LevelUpMenu>();
        if (levelUp != null)
        {
            levelUp.enabled = false;
            Destroy(levelUp);
        }

        var menu = go.GetComponent<SkillTreeMenu>();
        if (menu == null)
            menu = go.AddComponent<SkillTreeMenu>();
        menu.ConvertFromLevelUp();
        menu.BuildSkillTree();
        menu.Setup(world);
        return menu;
    }

    private static GameObject FindLevelUpPrefab(Transform parent)
    {
        var manager = parent.GetComponent<GameMenuManager>();
        if (manager == null)
            manager = parent.GetComponentInParent<GameMenuManager>();
        if (manager?.menus == null)
            return null;
        for (int i = 0; i < manager.menus.Length; i++)
        {
            if (manager.menus[i] != null && manager.menus[i].GetComponent<LevelUpMenu>() != null)
                return manager.menus[i];
        }
        return null;
    }

    private void ConvertFromLevelUp()
    {
        SetChildActive("Stats", false);
        SetChildActive("Confirm", true);
        SetChildActive("Cancel", true);

        contentRoot = FindChild(transform, "Content");
        if (contentRoot == null)
            contentRoot = transform;

        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name == "Title" || (tmp.transform.parent != null && tmp.transform.parent.name == "Title"))
                tmp.text = "Skill Tree";
            else if (tmp.gameObject.name == "Subtitle")
                subtitleLabel = tmp;
            else if (tmp.gameObject.name == "Soul Cost")
                costLabel = tmp;
        }

        if (subtitleLabel != null)
        {
            subtitleLabel.enableWordWrapping = true;
            subtitleLabel.overflowMode = TextOverflowModes.Overflow;
            var subRect = subtitleLabel.rectTransform;
            subRect.sizeDelta = new Vector2(320f, 40f);
        }

        if (costLabel != null)
        {
            costLabel.enableWordWrapping = false;
            costLabel.overflowMode = TextOverflowModes.Overflow;
            var costRect = costLabel.rectTransform;
            costRect.sizeDelta = new Vector2(200f, 36f);
        }

        WireNamedButton("Confirm", Confirm);
        WireNamedButton("Cancel", Cancel);
    }

    private void WireNamedButton(string name, UnityEngine.Events.UnityAction action)
    {
        var child = FindChild(transform, name);
        if (child == null) return;
        child.gameObject.SetActive(true);
        var button = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        var nav = Navigation.defaultNavigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
    }

    private const float FirstIconY = -110f;
    private const float TalismanHeaderY = -382f;
    private const float TalismanScale = 0.88f;

    private void BuildSkillTree()
    {
        float col = 88f;
        float step = iconSize + iconGap;
        for (int i = 0; i < 4; i++)
        {
            float y = FirstIconY - i * step;
            CreateNodeIcon((SkillTreeNode)i, new Vector2(-col, y));
            CreateNodeIcon((SkillTreeNode)(i + 4), new Vector2(col, y));
        }

        AddHeader("Talisman", new Vector2(0, TalismanHeaderY), numberFont, headerFontSize * TalismanScale);
    }

    public override void Setup(World world)
    {
        base.Setup(world);
        CreateSocketSlot();
        Refresh();
    }

    private void CreateSocketSlot()
    {
        if (world?.gameManager?.ui?.playerSlots == null || world.gameManager.ui.playerSlots.Length == 0)
            return;
        var slots = world.gameManager.ui.playerSlots;
        var template = slots.Length > 4 ? slots[4] : slots[0];
        socketSlot = Instantiate(template, contentRoot);
        socketSlot.item = Item.Blank;
        if (socketSlot.itemDisplay != null)
            socketSlot.itemDisplay.SetItem(Item.Blank);
        var rect = socketSlot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float slotSize = iconSize * TalismanScale;
        rect.sizeDelta = new Vector2(slotSize, slotSize);
        float y = TalismanHeaderY - headerFontSize * TalismanScale - 4f;
        rect.anchoredPosition = new Vector2(0, y);
        rect.SetAsLastSibling();
        socketSlot.swapper = template.swapper;
        socketSlot.tooltipManager = template.tooltipManager;
        if (socketSlot.itemDisplay != null)
        {
            socketSlot.itemDisplay.showTierLabel = false;
            socketSlot.itemDisplay.genericPlaceholder = true;
            socketSlot.itemDisplay.placeholderOverride = LoadSprite("TalismanIcon", "Assets/Sprites/SkillTree/TalismanIcon.png");
        }
        foreach (var tmp in socketSlot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (socketSlot.itemDisplay != null && tmp == socketSlot.itemDisplay.cornerText)
                continue;
            tmp.gameObject.SetActive(false);
        }
        foreach (var graphic in socketSlot.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic is TextMeshProUGUI)
                continue;
            graphic.raycastTarget = true;
        }
        var slotNav = Navigation.defaultNavigation;
        slotNav.mode = Navigation.Mode.None;
        foreach (var slotButton in socketSlot.GetComponentsInChildren<Button>(true))
            slotButton.navigation = slotNav;
        socketSlot.SetOwner(world.player, SkillTreeFunctions.Talisman_Slot);
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            hoverNode = -1;
            HideNodeTooltip();
        });
        var trigger = socketSlot.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = socketSlot.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Add(enter);
        if (socketSlot.itemDisplay != null)
        {
            if (socketSlot.itemDisplay.placeholder != null)
            {
                socketSlot.itemDisplay.placeholder.color = Color.white;
                socketSlot.itemDisplay.placeholder.material = null;
            }
            if (socketSlot.itemDisplay.itemImage != null)
                socketSlot.itemDisplay.itemImage.color = Color.white;
        }
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void OnDisable()
    {
        HideNodeTooltip();
    }

    private void OnDestroy()
    {
        HideNodeTooltip();
        if (nodeTooltip != null)
        {
            Destroy(nodeTooltip.gameObject);
            nodeTooltip = null;
        }
    }

    private void Refresh()
    {
        var player = world?.player;
        if (player == null) return;

        int spent = SkillTreeFunctions.GetSpentTotal(player.skillTreeRanks) + GetPendingTotal();
        if (subtitleLabel != null)
            subtitleLabel.text = $"Pick skills to enhance your ability. Points: {spent}/{SkillTreeFunctions.Point_Cap}";
        UpdateCost();

        for (int i = 0; i < SkillTreeFunctions.Node_Count; i++)
        {
            var node = (SkillTreeNode)i;
            int rank = SkillTreeFunctions.GetSpentRank(player.skillTreeRanks, node) + pendingRanks[i];
            int gear = player.GetGearTalentRank(node);
            if (rankLabels[i] == null) continue;
            rankLabels[i].text = gear > 0 ? $"{rank}+{gear}" : rank.ToString();
            if (rank >= SkillTreeFunctions.Max_Spent_Rank)
                rankLabels[i].color = Color.yellow;
            else if (pendingRanks[i] > 0)
                rankLabels[i].color = Color.green;
            else if (rank > 0)
                rankLabels[i].color = Color.green;
            else
                rankLabels[i].color = Color.white;
            if (nodeIcons[i] != null)
                nodeIcons[i].sprite = GetNodeSprite(GetPlayerClass(), node);
        }

        if (socketSlot != null)
            socketSlot.SetItem(player.GetItem(SkillTreeFunctions.Talisman_Slot));

        if (hoverNode >= 0)
        {
            hideHoverFrames = 0;
            ShowNodeTooltip(player);
        }
        else if (nodeTooltip != null && nodeTooltip.gameObject.activeSelf)
        {
            hideHoverFrames++;
            if (hideHoverFrames > 2)
                HideNodeTooltip();
        }
    }

    private void ShowNodeTooltip(Player player)
    {
        var manager = world?.gameManager?.ui?.tooltipManager;
        if (manager == null) return;

        if (nodeTooltip == null)
        {
            var template = FindItemTooltip(manager);
            if (template == null) return;
            var copy = Instantiate(template.gameObject, manager.transform);
            copy.transform.SetAsLastSibling();
            copy.SetActive(false);
            nodeTooltip = copy.GetComponent<ItemTooltip>();
            nodeTooltip.tooltipManager = manager;
            tooltipKey = int.MinValue;
        }

        var node = (SkillTreeNode)hoverNode;
        int rank = SkillTreeFunctions.GetSpentRank(player.skillTreeRanks, node) + pendingRanks[hoverNode];
        int key = hoverNode * 1000 + rank * 10 + player.GetGearTalentRank(node);
        bool needsRebuild = key != tooltipKey || !nodeTooltip.gameObject.activeSelf;
        if (!needsRebuild)
        {
            nodeTooltip.PositionAtMouse();
            return;
        }
        tooltipKey = key;
        int gear = player.GetGearTalentRank(node);
        var classType = (ClassType)player.info.id;
        var body = BuildNodeTooltipBody(classType, node, rank, gear);
        nodeTooltip.gameObject.SetActive(true);
        nodeTooltip.ApplySkillTreeNode(GetNodeSprite(classType, node), SkillTreeFunctions.GetNodeName(classType, node), body);
        Canvas.ForceUpdateCanvases();
        nodeTooltip.RefitSkillTreeLayout();
    }

    private static string BuildNodeTooltipBody(ClassType classType, SkillTreeNode node, int spent, int gear)
    {
        int nowRank = SkillTreeFunctions.ClampEffective(spent, gear);
        int nextRank = nowRank + 1;
        var gearLine = gear > 0 ? $"  (gear +{gear})" : "";
        var text = $"Rank: {spent}/{SkillTreeFunctions.Max_Spent_Rank}{gearLine}\n";
        text += "<line-height=80%><size=16.67%><sprite=\"LabelSprites\" name=\"Line\" color=#000000>\n</size></line-height>";
        text += $"Now: {SkillTreeFunctions.GetNodeEffect(classType, node, nowRank)}\n";
        text += $"Next: {SkillTreeFunctions.GetNodeEffect(classType, node, nextRank)}\n";
        text += "<line-height=80%><size=16.67%><sprite=\"LabelSprites\" name=\"Line\" color=#000000></size></line-height>\n";
        text += $"<indent=0>{EffectStyleFunctions.ToRichText(SkillTreeFunctions.GetNodeStyle(classType, node))}";
        return text;
    }

    private static ItemTooltip FindItemTooltip(TooltipManager manager)
    {
        if (manager?.tooltips == null) return null;
        for (int i = 0; i < manager.tooltips.Length; i++)
        {
            var item = manager.tooltips[i] as ItemTooltip;
            if (item != null)
                return item;
        }
        return null;
    }

    private void HideNodeTooltip()
    {
        hideHoverFrames = 0;
        tooltipKey = int.MinValue;
        if (nodeTooltip == null) return;
        nodeTooltip.gameObject.SetActive(false);
    }

    private void CreateNodeIcon(SkillTreeNode node, Vector2 anchoredPos)
    {
        var go = new GameObject(node.ToString(), typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(contentRoot, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(iconSize, iconSize);
        rect.anchoredPosition = anchoredPos;

        var image = go.AddComponent<Image>();
        image.preserveAspect = true;
        image.sprite = GetNodeSprite(GetPlayerClass(), node);
        image.color = Color.white;
        image.raycastTarget = true;
        ApplyItemIconMaterial(image);
        nodeIcons[(int)node] = image;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var nav = Navigation.defaultNavigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        int index = (int)node;
        button.onClick.AddListener(() => OnNodeClicked(index));

        var hover = go.AddComponent<SkillTreeNodeHover>();
        hover.menu = this;
        hover.nodeIndex = index;

        var rankGo = new GameObject("Rank", typeof(RectTransform));
        var rankRect = rankGo.GetComponent<RectTransform>();
        rankRect.SetParent(rect, false);
        rankRect.anchorMin = Vector2.zero;
        rankRect.anchorMax = Vector2.one;
        rankRect.offsetMin = Vector2.zero;
        rankRect.offsetMax = Vector2.zero;
        var rankTmp = rankGo.AddComponent<TextMeshProUGUI>();
        rankTmp.alignment = TextAlignmentOptions.BottomRight;
        rankTmp.fontSize = rankFontSize;
        rankTmp.color = Color.white;
        rankTmp.raycastTarget = false;
        if (numberFont != null)
            rankTmp.font = numberFont;
        rankLabels[(int)node] = rankTmp;
    }

    private static void ApplyItemIconMaterial(Image image)
    {
        if (image == null || MaterialManager.spriteUIs == null || MaterialManager.spriteUIs.Length == 0)
            return;
        image.material = MaterialManager.GetUIMaterial(0);
    }

    private void OnNodeClicked(int index)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        var player = world?.player;
        if (player == null) return;
        if (!SkillTreeFunctions.IsUnlocked(player.GetLevel())) return;

        var node = (SkillTreeNode)index;
        int spent = SkillTreeFunctions.GetSpentRank(player.skillTreeRanks, node);
        int queued = pendingRanks[index];
        if (spent + queued >= SkillTreeFunctions.Max_Spent_Rank) return;
        if (SkillTreeFunctions.GetSpentTotal(player.skillTreeRanks) + GetPendingTotal() >= SkillTreeFunctions.Point_Cap) return;

        pendingRanks[index] = queued + 1;
        hoverNode = index;
        tooltipKey = int.MinValue;
        Refresh();
    }

    public void Confirm()
    {
        var player = world?.player;
        if (player == null) return;
        if (!SkillTreeFunctions.IsUnlocked(player.GetLevel())) return;
        int total = GetPendingCost();
        if (total <= 0)
        {
            Close();
            return;
        }
        if (player.fullSouls < total) return;

        for (int i = 0; i < SkillTreeFunctions.Node_Count; i++)
        {
            int queued = pendingRanks[i];
            for (int n = 0; n < queued; n++)
            {
                var node = (SkillTreeNode)i;
                int spent = SkillTreeFunctions.GetSpentRank(player.skillTreeRanks, node);
                if (spent >= SkillTreeFunctions.Max_Spent_Rank) break;
                player.skillTreeRanks = SkillTreeFunctions.SetSpentRank(player.skillTreeRanks, node, spent + 1);
                world.gameManager.client.SendAsync(new TnUnlockTalent((byte)i));
            }
            pendingRanks[i] = 0;
        }
        Close();
    }

    public void Cancel()
    {
        Close();
    }

    private int GetPendingTotal()
    {
        int total = 0;
        for (int i = 0; i < pendingRanks.Length; i++)
            total += pendingRanks[i];
        return total;
    }

    private int GetPendingCost()
    {
        var player = world?.player;
        if (player == null) return 0;
        int total = 0;
        for (int i = 0; i < SkillTreeFunctions.Node_Count; i++)
        {
            int spent = SkillTreeFunctions.GetSpentRank(player.skillTreeRanks, (SkillTreeNode)i);
            for (int n = 1; n <= pendingRanks[i]; n++)
                total += SkillTreeFunctions.GetRankCost(spent + n);
        }
        return total;
    }

    private void UpdateCost()
    {
        if (costLabel == null) return;
        int total = GetPendingCost();
        costLabel.text = $"<color=#ffffff>Cost:  </color>{Constants.Souls_Sprite}{total}";
        var player = world?.player;
        if (player != null && player.fullSouls < total)
            costLabel.text += "\n<color=#ff0000>Not enough essence</color>";
    }

    public void SetHover(int nodeIndex)
    {
        hoverNode = nodeIndex;
    }

    private void AddHeader(string text, Vector2 pos, TMP_FontAsset font, float fontSize)
    {
        var go = new GameObject(text, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(contentRoot, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(160, fontSize + 6);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        if (font != null)
            tmp.font = font;
    }

    private void SetChildActive(string name, bool active)
    {
        var child = FindChild(transform, name);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChild(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private ClassType GetPlayerClass()
    {
        if (world?.player != null)
            return (ClassType)world.player.info.id;
        return ClassType.Warrior;
    }

    private Sprite GetNodeSprite(ClassType classType, SkillTreeNode node)
    {
        var key = SkillTreeFunctions.GetNodeSprite(classType, node);
        return LoadSprite(key, $"Assets/Sprites/SkillTree/{key}.png");
    }

    public static Sprite LoadHudSprite()
    {
        return LoadSprite("SkillTreeMenu", "Assets/Sprites/SkillTree/SkillTreeMenu.png");
    }

    private static readonly Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();

    private static Sprite LoadSprite(string name, string editorPath)
    {
        if (loadedSprites.TryGetValue(name, out var cached) && cached != null)
            return cached;

        Texture2D tex = null;
#if UNITY_EDITOR
        tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(editorPath);
#endif
        if (tex == null)
        {
            string resPath = "Sprites/SkillTree/" + name;
            tex = Resources.Load<Texture2D>(resPath);
        }
        if (tex == null)
            return null;

        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        loadedSprites[name] = sprite;
        return sprite;
    }
}

public class SkillTreeHudIcon : MonoBehaviour
{
    private void LateUpdate()
    {
        var image = GetComponent<Image>();
        var sprite = SkillTreeMenu.LoadHudSprite();
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
            enabled = false;
        }
    }
}

public class SkillTreeNodeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillTreeMenu menu;
    public int nodeIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        menu?.SetHover(nodeIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        menu?.SetHover(-1);
    }
}
