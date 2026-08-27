#if UNITY_STANDALONE
using Steamworks;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Data;
using TitanCore.Iap;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;

public class GameDataLoader : MonoBehaviour
{
#if UNITY_STANDALONE

    public static DiscordManager discordManager;

#endif

    /// <summary>
    /// All game sprites
    /// </summary>
    public SpriteAtlas[] spriteAtlases;

    /// <summary>
    /// All game meshes
    /// </summary>
    public Mesh[] meshes;

    public TextAsset[] xmls;

    private Dictionary<string, TextAsset> xmlMap = new Dictionary<string, TextAsset>();

    public Sprite[] uiSprites;

    public Texture2D meshTexture;

    private void Awake()
    {
        bool isOther = FindObjectsOfType<GameDataLoader>().Any(_ => _ != this);
        if (isOther)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        BuildXmlMap();
        LoadGameData();
        MeshManager.Init(meshTexture, meshes);
        TextureManager.Init(spriteAtlases);
        TextureManager.SetUISprites(uiSprites);
        AnimationManager.Init();

#if UNITY_STANDALONE

        discordManager = new DiscordManager();
        discordManager.Init();

        if (!Constants.Use_Local_Free_Store && Constants.Store_Type == StoreType.Steam)
        {
            try
            {
                SteamClient.Init(949430);
            }
            catch (Exception)
            {
                // Couldn't init for some reason (steam is closed etc)
            }
        }

#endif
    }

    /// <summary>
    /// Turns the Inspector-assigned `xmls` array into a name -> asset lookup.
    ///
    /// This used to be a one-line ToDictionary, which throws if the array contains a
    /// duplicate file name or an empty slot. Both are easy to cause by accident when
    /// dragging assets into the Inspector, and the resulting exception fired inside
    /// Awake, leaving the whole game half-initialised with no useful message.
    /// Now each bad entry is reported and skipped.
    /// </summary>
    private void BuildXmlMap()
    {
        xmlMap.Clear();

        if (xmls == null)
        {
            Debug.LogError("[GameDataLoader] The 'xmls' list on the GameDataLoader object is empty. No game data can be loaded.");
            return;
        }

        for (int i = 0; i < xmls.Length; i++)
        {
            var asset = xmls[i];

            // An empty Inspector slot. Consequence: whatever data file was meant to be
            // there is missing, which LoadGameData will report by name below.
            if (asset == null)
            {
                Debug.LogError($"[GameDataLoader] 'xmls' slot {i} is empty - skipping it.");
                continue;
            }

            if (xmlMap.ContainsKey(asset.name))
            {
                Debug.LogError($"[GameDataLoader] Duplicate data file '{asset.name}' in 'xmls' - keeping the first one.");
                continue;
            }

            xmlMap.Add(asset.name, asset);
        }
    }

    private void LoadGameData()
    {
        //BetterStreamingAssets.Initialize();
        GameData.ClearObjects();

        Debug.Log("Loading game data...");

        // GameData.LoadFiles names any file it cannot parse and then throws, because a
        // client running on partial item/enemy data would silently disagree with the
        // server about object ids. Catching it here keeps the exception from escaping
        // Awake mid-way through setup, and logs something a designer can act on.
        try
        {
            GameData.LoadFiles(xmlMap.Keys, LoadResource);
            Debug.Log($"Loaded {GameData.objects.Count} objects");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameDataLoader] Game data failed to load: {e.Message}");
        }
    }

    /// <summary>
    /// Hands GameData the bytes for one data file. Returning null instead of indexing
    /// blindly lets GameData report the file by name rather than throwing a
    /// KeyNotFoundException from inside the loader.
    /// </summary>
    private Stream LoadResource(string name)
    {
        if (!xmlMap.TryGetValue(name, out var asset) || asset == null)
        {
            Debug.LogError($"[GameDataLoader] Data file '{name}' is missing from the loader.");
            return null;
        }
        return new MemoryStream(asset.bytes);
    }

    /*
#if UNITY_ANDROID

    private static string[] assets = new string[]
    {
        "/data/characters.xml",
        "/data/enemies.xml",
        "/data/items.xml",
        "/data/lootbags.xml",
        "/data/pets.xml",
        "/data/projectiles.xml",
        "/data/skins.xml",
        "/data/staticobjects.xml",
        "/data/tiles.xml",
    };


    private void LoadGameData()
    {
        //BetterStreamingAssets.Initialize();
        GameData.ClearObjects();

        Debug.Log("Loading game data...");
        GameData.LoadFiles(assets, ReadAndroid);
        Debug.Log($"Loaded {GameData.objects.Count} objects");
    }

    private Stream ReadAndroid(string path)
    {
        path = Application.streamingAssetsPath + path;
        var webRequest = UnityWebRequest.Get(path);
        webRequest.SendWebRequest();

        while (!webRequest.isDone)
        {
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                break;
            }
        }

        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.LogError(path);
            Debug.LogError(webRequest.error);
            return null;
        }
        else
        {
            var data = webRequest.downloadHandler.data;
            return new MemoryStream(data);
        }
    }
#else
    private void LoadGameData()
    {
        GameData.ClearObjects();

        Debug.Log("Loading game data...");
        var streamingPath = Application.streamingAssetsPath;
        GameData.LoadDirectory(Path.Combine(Application.streamingAssetsPath, "data"));
        Debug.Log($"Loaded {GameData.objects.Count} objects");
    }
#endif
*/

#if UNITY_STANDALONE

    private void Update()
    {
        discordManager.Update();

        if (!Constants.Use_Local_Free_Store && Constants.Store_Type == StoreType.Steam)
        {
            SteamClient.RunCallbacks();
        }
    }

    private void OnApplicationQuit()
    {
        discordManager?.OnApplicationQuit();

        if (!Constants.Use_Local_Free_Store && Constants.Store_Type == StoreType.Steam)
        {
            SteamClient.Shutdown();
        }
    }

#endif
}
