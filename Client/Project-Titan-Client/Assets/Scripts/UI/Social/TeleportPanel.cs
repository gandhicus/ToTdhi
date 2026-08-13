using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TeleportPanel : MonoBehaviour
{
    public World world;

    public TeleportOption[] options;

    private bool didDown;

    public void Show(Vector2 worldPosition, float sampleRadius)
    {
        var waypointCandidates = world.waypoints
            .Where(_ => (((Vector2)_.transform.position) - worldPosition).magnitude <= sampleRadius)
            .OrderBy(_ => (worldPosition - ((Vector2)_.transform.position)).sqrMagnitude)
            .ToArray();

        var characterCandidates = world.characters
            .Where(_ => _ != world.player && (((Vector2)_.transform.position) - worldPosition).magnitude <= sampleRadius)
            .OrderBy(_ => (worldPosition - ((Vector2)_.transform.position)).sqrMagnitude)
            .ToArray();

        if (waypointCandidates.Length == 0 && characterCandidates.Length == 0)
            return;

        didDown = false;
        var optionIndex = 0;
        for (int i = 0; i < waypointCandidates.Length && optionIndex < options.Length; i++, optionIndex++)
            options[optionIndex].Setup(waypointCandidates[i]);

        for (int i = 0; i < characterCandidates.Length && optionIndex < options.Length; i++, optionIndex++)
            options[optionIndex].Setup(characterCandidates[i]);

        for (; optionIndex < options.Length; optionIndex++)
            options[optionIndex].Setup((Character)null);

        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!didDown)
        {
            didDown = Input.GetMouseButtonDown(0);
        }

        if (didDown && Input.GetMouseButtonUp(0))
        {
            gameObject.SetActive(false);
        }
    }
}
