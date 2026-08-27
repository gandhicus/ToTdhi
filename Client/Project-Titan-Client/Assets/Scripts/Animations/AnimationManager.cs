using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Data;
using TitanCore.Data.Components.Textures;

public static class AnimationManager
{
    /// <summary>
    /// Dictionary containing all animations
    /// </summary>
    private static Dictionary<TextureData, Animation> animations = new Dictionary<TextureData, Animation>();

    /// <summary>
    /// Initializes all stored animation data.
    ///
    /// Runs once at client startup over every loaded game object. Each animation is
    /// built inside its own try/catch: one object with bad texture data should cost you
    /// that object's animation, not every animation defined after it in the load order.
    /// </summary>
    public static void Init()
    {
        animations.Clear();

        foreach (var o in GameData.objects.Values) // loop all objects
        {
            if (o.textures == null) continue;
            foreach (var textureData in o.textures) // loop the textures of each object
            {
                if (textureData == null) continue;

                try
                {
                    CreateAnimation(textureData);
                }
                catch (Exception e)
                {
                    // Consequence of landing here: GetAnimation will later return null for
                    // this texture and the object renders as a still sprite.
                    UnityEngine.Debug.LogError($"[AnimationManager] Failed to build animation for '{o.name}' (texture '{textureData.displaySprite}'): {e.Message}");
                }
            }
        }
    }

    private static void CreateAnimation(TextureData textureData)
    {
        switch (textureData)
        {
            case CharacterTextureData charTextureData:
                CreateCharacterAnimation(charTextureData);
                break;
            case EntityTextureData entityTextureData:
                CreateEntityAnimation(entityTextureData);
                break;
            case SequenceTextureData seqTextureData:
                CreateSequenceAnimation(seqTextureData);
                break;
        }
    }

    /// <summary>
    /// Creates a character animation and stores it in the animation dictionary
    /// </summary>
    /// <param name="textureData"></param>
    private static void CreateCharacterAnimation(CharacterTextureData textureData)
    {
        Store(textureData, new CharacterAnimation(textureData));
    }

    /// <summary>
    /// Creates an entity animation and stores it in the animation dictionary
    /// </summary>
    /// <param name="textureData"></param>
    private static void CreateEntityAnimation(EntityTextureData textureData)
    {
        Store(textureData, new EntityAnimation(textureData));
    }

    /// <summary>
    /// Creates a sequence animation and stores it in the animation dictionary
    /// </summary>
    /// <param name="textureData"></param>
    private static void CreateSequenceAnimation(SequenceTextureData textureData)
    {
        Store(textureData, new SequenceAnimation(textureData));
    }

    /// <summary>
    /// Adds a built animation to the cache.
    ///
    /// Dictionary.Add throws on a repeated key, so the same TextureData appearing twice
    /// in an object's texture list used to abort Init and leave the animation cache
    /// half-built for every object after it. Skipping the repeat is harmless because
    /// both entries would describe the same animation anyway.
    /// </summary>
    private static void Store(TextureData textureData, Animation animation)
    {
        if (animations.ContainsKey(textureData))
        {
            UnityEngine.Debug.LogError($"[AnimationManager] Duplicate animation for texture '{textureData.displaySprite}' - keeping the first one.");
            return;
        }
        animations.Add(textureData, animation);
    }

    /// <summary>
    /// Returns the built animation for a given texture data, or null if there is none.
    ///
    /// This deliberately mirrors TextureManager.GetSprite, which already returns null
    /// for a missing sprite. Previously this used a bare dictionary index, so a texture
    /// entry the manager never built (an unsupported type, or one skipped by an earlier
    /// failure) threw a KeyNotFoundException from whatever object happened to spawn
    /// first. Callers already handle a null animation by falling back to a static
    /// sprite, so returning null degrades to "this thing does not animate" instead of
    /// taking the client down.
    /// </summary>
    public static Animation GetAnimation(TextureData textureData)
    {
        if (textureData == null)
            return null;

        if (!animations.TryGetValue(textureData, out var animation))
        {
            UnityEngine.Debug.LogError($"[AnimationManager] No animation was built for texture '{textureData.displaySprite}'. It will render without animating.");
            return null;
        }
        return animation;
    }
}
