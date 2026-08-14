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
        if (onlyWhenCanLevelUp)
            gameManager = FindObjectOfType<GameManager>();
    }

    private void LateUpdate()
    {
        if (onlyWhenCanLevelUp)
        {
            var player = gameManager?.world?.player;
            if (player == null || !player.CanLevelUp())
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
