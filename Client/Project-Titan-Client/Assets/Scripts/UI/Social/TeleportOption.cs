using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Net.Packets.Client;
using TMPro;
using UnityEngine;

public class TeleportOption : MonoBehaviour
{
    public World world;

    public ClassPreview preview;

    public TextMeshProUGUI nameLabel;

    private Character character;

    private Waypoint waypoint;

    public void Setup(Character character)
    {
        waypoint = null;

        if (character == null)
        {
            gameObject.SetActive(false);
            this.character = null;
            return;
        }

        this.character = character;
        gameObject.SetActive(true);

        preview.SetClass(character.GetSkinInfo());
        nameLabel.text = character.playerName;
    }

    public void Setup(Waypoint waypoint)
    {
        character = null;

        if (waypoint == null)
        {
            gameObject.SetActive(false);
            this.waypoint = null;
            return;
        }

        this.waypoint = waypoint;
        gameObject.SetActive(true);

        preview.image.sprite = TextureManager.GetDisplaySprite(waypoint.info);
        nameLabel.text = waypoint.waypointName;
    }

    public void Select()
    {
        if (waypoint != null)
            world.TeleportToWaypoint(waypoint);
        else
            world.Teleport(character);
    }
}
