using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Core;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Client;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AscensionMenu : GameMenu
{
    public override MenuType MenuType => MenuType.Ascension;

    public Button maxHealthButton;

    public Button speedButton;

    public Button attackButton;

    public Button defenseButton;

    public Button vigorButton;

    public TextMeshProUGUI maxHealthCost;

    public TextMeshProUGUI speedCost;

    public TextMeshProUGUI attackCost;

    public TextMeshProUGUI defenseCost;

    public TextMeshProUGUI vigorCost;

    public ItemDisplay maxHealthItemCost;

    public ItemDisplay speedItemCost;

    public ItemDisplay attackItemCost;

    public ItemDisplay defenseItemCost;

    public ItemDisplay vigorItemCost;

    private Player player;

    private uint tableGameId;

    public override void Setup(World world)
    {
        base.Setup(world);

        var rectTransform = (RectTransform)transform;
#if UNITY_IOS || UNITY_ANDROID
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
#else
        rectTransform.anchoredPosition = new Vector2(Screen.width - Screen.height * 0.26f, 0);
#endif

        OnEnable();
    }

    public void SetGameId(uint gameId)
    {
        tableGameId = gameId;
    }

    private void OnEnable()
    {
        if (world == null) return;
        player = world.player;

        UpdateLabels();
    }

    private void LateUpdate()
    {
        if (player == null) return;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        var nextGearGains = GetNextGearGains();
        UpdateLabel(StatType.MaxHealth, maxHealthButton, maxHealthCost, maxHealthItemCost, nextGearGains);
        UpdateLabel(StatType.Speed, speedButton, speedCost, speedItemCost, nextGearGains);
        UpdateLabel(StatType.Attack, attackButton, attackCost, attackItemCost, nextGearGains);
        UpdateLabel(StatType.Defense, defenseButton, defenseCost, defenseItemCost, nextGearGains);
        UpdateLabel(StatType.Vigor, vigorButton, vigorCost, vigorItemCost, nextGearGains);
    }

    private Dictionary<StatType, int> GetNextGearGains()
    {
        var equips = new Item[4];
        for (int i = 0; i < equips.Length; i++)
            equips[i] = player.GetItem(i);

        var ascensionCounts = player.GetAscensionCounts();
        var scalingBonusStats = player.GetScalingBonusStats();
        var scalingBonusAlternateStats = player.GetScalingBonusAlternateStats();
        var stats = new Dictionary<StatType, int>();
        var alternateStats = new Dictionary<AlternateStatType, int>();
        var nextGearGains = new Dictionary<StatType, int>();

        EquipmentStatFunctions.RecalculateEquipmentStats(equips, stats, alternateStats, scalingBonusStats, scalingBonusAlternateStats, ascensionCounts);
        var currentStats = new Dictionary<StatType, int>(stats);

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            int currentRank = ascensionCounts[statType];
            if (currentRank >= NetConstants.Max_Ascension) continue;

            ascensionCounts[statType] = currentRank + 1;
            EquipmentStatFunctions.RecalculateEquipmentStats(equips, stats, alternateStats, scalingBonusStats, scalingBonusAlternateStats, ascensionCounts);
            currentStats.TryGetValue(statType, out var currentGain);
            stats.TryGetValue(statType, out var nextGain);
            nextGearGains[statType] = nextGain - currentGain;
            ascensionCounts[statType] = currentRank;
        }

        return nextGearGains;
    }

    private void UpdateLabel(StatType statType, Button button, TextMeshProUGUI cost, ItemDisplay itemCostDisplay, IReadOnlyDictionary<StatType, int> nextGearGains)
    {
        var level = player.GetStatLevel();

        if (level < NetConstants.Max_Level)
        {
            button.interactable = false;
            cost.text = $"Level {NetConstants.Max_Level}\nrequired";
            cost.color = Color.red;
            return;
        }

        var baseStat = player.GetStatBase(statType);
        var statLock = player.GetStatLock(statType);

        if (statLock <= 0)
        {
            button.interactable = false;
            cost.text = "Stats not\nlocked yet";
            cost.color = Color.red;
            return;
        }

        if (player.GetAscensionCount(statType) >= NetConstants.Max_Ascension)
        {
            button.interactable = false;
            cost.text = "Stat ascension\nmaxed";
            cost.color = Stat.MaxAscendColor;
            return;
        }

        button.interactable = true;
        cost.color = Color.white;

        var soulCost = StatFunctions.GetAscensionCost(statType, baseStat, statLock, out var itemCost);
        if (soulCost <= 0)
        {
            button.interactable = false;
            cost.text = "Ascension\nunavailable";
            cost.color = Color.red;
            return;
        }
        nextGearGains.TryGetValue(statType, out var nextGearGain);
        cost.text = $"<size=125%>{Constants.Souls_Sprite}</size> {soulCost}\n<color=#6AECE1>Next: +{nextGearGain}</color>";
        itemCostDisplay.SetItem(itemCost);
    }

    public void Cancel()
    {
        Close();
    }

    public void AscendStat(int stat)
    {
        var statType = (StatType)stat;
        var level = player.GetStatLevel();
        var baseStat = player.GetStatBase(statType);
        var statLock = player.GetStatLock(statType);

        if (player.GetAscensionCount(statType) >= NetConstants.Max_Ascension)
        {
            world.GameChat($"You have already maxed {statType} ascension!", ChatType.Error);
            return;
        }

        if (level < NetConstants.Max_Level)
        {
            world.GameChat($"You must be level {NetConstants.Max_Level} to ascend your stats!", ChatType.Error);
            return;
        }

        var soulCost = StatFunctions.GetAscensionCost(statType, baseStat, statLock, out var itemCost);
        if (soulCost <= 0)
        {
            world.GameChat($"{statType} ascension is unavailable right now", ChatType.Error);
            return;
        }

        if (soulCost > player.fullSouls)
        {
            world.GameChat($"You do not have enough essence to ascend {statType}", ChatType.Error);
            return;
        }

        int itemCount = 0;
        for (int i = 4; i < 12; i++) // check item costs
        {
            var item = player.GetItem(i);
            if (item.id != itemCost.id) continue;
            itemCount += item.count;
        }

        if (itemCount < itemCost.count)
        {
            world.GameChat($"You do not have the required scrolls to ascend {statType}", ChatType.Error);
            return;
        }

        world.gameManager.client.SendAsync(new TnAscendStat(tableGameId, statType));
    }
}
