using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mobile
{
    public class WaypointTooltipMobile : MobileTooltip<Waypoint>
    {
        public Image waypointSprite;

        public TextMeshProUGUI nameLabel;

        private Waypoint waypoint;

        private World world;

        protected override void Load(Player player, bool owned, Waypoint obj)
        {
            world = player.world;
            waypoint = obj;

            waypointSprite.sprite = TextureManager.GetDisplaySprite(waypoint.info);
            nameLabel.text = obj.waypointName;
        }

        public void Teleport()
        {
            world.TeleportToWaypoint(waypoint);
            CloseTooltip();

            var mobileUI = (MobileGameUI)world.gameManager.ui;
            mobileUI.sideMenuManager.CloseMenu();
        }
    }
}
