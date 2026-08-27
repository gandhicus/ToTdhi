using System.Collections;
using System.Collections.Generic;
using TitanCore.Data;
using UnityEngine;
using UnityEngine.UI;

public class ClassPreview : MonoBehaviour
{
    public Image image;

    public AnimationDirection direction = AnimationDirection.Down;

    public AnimationState state = AnimationState.All;

    public bool animated = false;

    public ushort defaultClass = 0;

    public RectTransform rectTransform;

    private Animation currentAnimation;

    private float frameTime = 0.25f;

    private int currentFrame = 0;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (defaultClass != 0)
        {
            // Indexing GameData.objects directly threw if the class id set on this
            // component no longer exists in characters.xml, which took out the whole
            // character-select screen rather than just this one portrait.
            if (!GameData.objects.TryGetValue(defaultClass, out var info))
            {
                Debug.LogError($"[ClassPreview] Class id {defaultClass} is not in the game data. This preview will stay blank.");
                return;
            }
            SetClass(info);
        }
    }

    /// <summary>
    /// Points this preview at a class's first animation. Every step is checked because
    /// AnimationManager.GetAnimation can now legitimately return null for content whose
    /// animation could not be built, and a blank portrait is a much better outcome than
    /// an exception during UI setup.
    /// </summary>
    public void SetClass(GameObjectInfo info)
    {
        currentAnimation = null;

        if (info?.textures == null || info.textures.Length == 0)
        {
            Debug.LogError($"[ClassPreview] Class '{info?.name}' has no textures defined, so it cannot be previewed.");
            return;
        }

        currentAnimation = AnimationManager.GetAnimation(info.textures[0]);
        ResetFrame();
    }

    public void ResetFrame()
    {
        currentFrame = 0;
        frameTime = 0.25f;

        UpdateFrame();
    }

    private void LateUpdate()
    {
        if (!animated || currentAnimation == null) return;

        frameTime -= Time.deltaTime;
        if (frameTime <= 0)
        {
            frameTime = 0.25f;
            currentFrame++;
            UpdateFrame();
        }
    }

    private void UpdateFrame()
    {
        // Nothing to draw is a valid state now: either no class has been set yet, or the
        // class's animation could not be built. Leaving the existing image alone is the
        // right behaviour - the alternative was a NullReferenceException every frame.
        if (currentAnimation == null) return;

        var frames = currentAnimation.GetFrames(AnimationState.All, direction);
        if (frames == null || frames.Length == 0) return;

        currentFrame = currentFrame % frames.Length;
        var sprite = frames[currentFrame];
        if (sprite == null) return;

        SetImage(sprite);
    }

    private void SetImage(Sprite sprite)
    {
        var aspect = sprite.textureRect.width / sprite.textureRect.height;
        var rect = rectTransform.rect;
        var scale = rect.height / sprite.textureRect.height;
        image.rectTransform.sizeDelta = new Vector2(rect.height * aspect, rect.height);
        image.rectTransform.anchoredPosition = new Vector2(-sprite.pivot.x * scale, 0);

        image.sprite = sprite;
    }
}
