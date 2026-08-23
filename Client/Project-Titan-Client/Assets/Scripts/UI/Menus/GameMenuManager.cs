using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Core;
using TitanCore.Net;
using UnityEngine;
using UnityEngine.UI;

public enum MenuType
{
    LevelUp,
    Vault,
    Ascension,
    Wardrobe,
    SkillTree
}

public abstract class GameMenu : MonoBehaviour
{
    public abstract MenuType MenuType { get; }

    public World world;

    public virtual void Setup(World world)
    {
        this.world = world;
    }

    public virtual void Close()
    {
        world.menuManager.CloseMenu(this);
    }
}

public class GameMenuManager : MonoBehaviour
{
    public World world;

    public GameObject[] menus;

    private Dictionary<MenuType, GameObject> prefabs;

    private Dictionary<MenuType, GameMenu> openMenus = new Dictionary<MenuType, GameMenu>();

    private GameObject skillTreeHudButton;

    private void Awake()
    {
        prefabs = menus.ToDictionary(_ => _.GetComponent<GameMenu>().MenuType);
    }

    private void Start()
    {
#if !UNITY_IOS && !UNITY_ANDROID
        CreateSkillTreeHudButton();
#endif
    }

    private void CreateSkillTreeHudButton()
    {
        if (!SkillTreeFunctions.IsEnabled) return;
        var levelUp = GameObject.Find("Button: Level Up");
        if (levelUp == null) return;

        var clone = Instantiate(levelUp, levelUp.transform.parent);
        clone.name = "Button: Skill Tree";
        var srcRect = levelUp.GetComponent<RectTransform>();
        var rect = clone.GetComponent<RectTransform>();
        float height = srcRect.anchorMax.y - srcRect.anchorMin.y;
        rect.anchorMin = new Vector2(srcRect.anchorMin.x, srcRect.anchorMin.y - height - 0.02f);
        rect.anchorMax = new Vector2(srcRect.anchorMax.x, srcRect.anchorMax.y - height - 0.02f);
        rect.anchoredPosition = srcRect.anchoredPosition;
        rect.sizeDelta = srcRect.sizeDelta;

        clone.AddComponent<SkillTreeHudIcon>();

        var flash = clone.GetComponent<Flash>();
        if (flash != null)
        {
            flash.onlyWhenCanLevelUp = false;
            flash.onlyWhenCanSpendSkillTree = true;
        }

        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ToggleSkillTree);
        skillTreeHudButton = clone;
        skillTreeHudButton.SetActive(false);
    }

    private void LateUpdate()
    {
        bool unlocked = world?.player != null && SkillTreeFunctions.IsUnlocked(world.player.GetLevel());
        if (skillTreeHudButton != null && skillTreeHudButton.activeSelf != unlocked)
            skillTreeHudButton.SetActive(unlocked);
        if (!unlocked && openMenus.TryGetValue(MenuType.SkillTree, out var open))
            CloseMenu(open);
    }

    public void ToggleMenu(MenuType type)
    {
        if (openMenus.TryGetValue(type, out var menu))
            CloseMenu(menu);
        else
            ShowMenu(type);
    }

    public GameMenu ShowMenu(MenuType type)
    {
        CloseExclusiveMenu(type);

        if (openMenus.TryGetValue(type, out var menu))
            return menu;

        var menuObject = Instantiate(prefabs[type]);
        var rect = menuObject.GetComponent<RectTransform>();
        rect.SetParent(transform);

#if UNITY_IOS || UNITY_ANDROID

#else
        rect.anchoredPosition = new Vector2(Screen.width * 0.78f, 0);
#endif

        menu = menuObject.GetComponent<GameMenu>();
        menu.Setup(world);
        openMenus.Add(menu.MenuType, menu);
        return menu;
    }

    public void CloseMenu(GameMenu menu)
    {
        openMenus.Remove(menu.MenuType);
        Destroy(menu.gameObject);
    }

    private void CloseExclusiveMenu(MenuType opening)
    {
        MenuType other;
        if (opening == MenuType.LevelUp)
            other = MenuType.SkillTree;
        else if (opening == MenuType.SkillTree)
            other = MenuType.LevelUp;
        else
            return;

        if (openMenus.TryGetValue(other, out var menu))
            CloseMenu(menu);
    }

    public void ToggleLevelUp()
    {
        if (!NetConstants.Use_Manual_Stat_Leveling) return;
        ToggleMenu(MenuType.LevelUp);
    }

    public void ToggleSkillTree()
    {
        if (!SkillTreeFunctions.IsEnabled) return;
        if (world?.player == null || !SkillTreeFunctions.IsUnlocked(world.player.GetLevel()))
            return;
        if (prefabs != null && prefabs.ContainsKey(MenuType.SkillTree))
        {
            ToggleMenu(MenuType.SkillTree);
            return;
        }
        if (openMenus.TryGetValue(MenuType.SkillTree, out var menu))
        {
            CloseMenu(menu);
            return;
        }
        CloseExclusiveMenu(MenuType.SkillTree);
        var created = SkillTreeMenu.CreateRuntime(transform, world);
        openMenus[MenuType.SkillTree] = created;
    }
}
