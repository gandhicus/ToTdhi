using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class Flash : MonoBehaviour
{
    public Color flash;

    [Tooltip("Only flash when the local player can afford a manual level up.")]
    public bool onlyWhenCanLevelUp;

    [Tooltip("Only flash when the local player can spend essence on the skill tree.")]
    public bool onlyWhenCanSpendSkillTree;

    private Graphic graphic;

    private Color graphicColor;

    private GameManager gameManager;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        graphicColor = graphic.color;
    }

    private void Start()
    {
        if (onlyWhenCanLevelUp || onlyWhenCanSpendSkillTree)
            gameManager = FindObjectOfType<GameManager>();
    }

    private void LateUpdate()
    {
        var player = gameManager?.world?.player;
        if (onlyWhenCanLevelUp)
        {
            if (player == null || !player.CanLevelUp())
            {
                graphic.color = graphicColor;
                return;
            }
        }
        if (onlyWhenCanSpendSkillTree)
        {
            if (player == null || !player.CanSpendSkillTreePoint())
            {
                graphic.color = graphicColor;
                return;
            }
        }

        graphic.color = Color.Lerp(graphicColor, flash, Mathf.Sin(Time.time * Mathf.PI) / 2f + 0.5f);
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            graphic.color = graphicColor;
        }
    }
}
