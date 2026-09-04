using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TitanCore.Core;
using TitanCore.Net;
using TitanCore.Net.Web;
using UnityEngine;

public static class WebClient
{
    public class Response<T>
    {
        public Exception exception;

        public T item;

        public Response(Exception exception)
        {
            this.exception = exception;
            item = default;
        }

        public Response(T item)
        {
            this.item = item;
            exception = null;
        }
    }

    private static string Web_Server_Url = "https://web.trialsoftitan.com/";

    private static string Local_Web_Server_Url = "http://localhost:8443/";

    private const string Local_Server_Config_File = "LocalServer.txt";

    private const string Local_Server_Host_Env = "TRIALS_LOCAL_SERVER_HOST";

    private const string Web_Server_Url_Env = "TRIALS_WEB_SERVER_URL";

    private static HttpClient client = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static string MakeQueryString(Dictionary<string, string> query)
    {
        var builder = new StringBuilder();
        foreach (var pair in query)
        {
            if (builder.Length > 0)
                builder.Append('&');
            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(pair.Value);
        }
        return builder.ToString();
    }

    private static async void SendRequest<T>(string path, Dictionary<string, string> query, Action<Response<T>> resultCallback)
    {
        T result = default;
        try
        {
            string url = GetConfiguredWebServerUrl() + path;

            var content = new FormUrlEncodedContent(query);
            var response = await client.PostAsync(url, content);
            var xml = await response.Content.ReadAsStringAsync();
            var ser = new XmlSerializer(typeof(T));
            result = (T)ser.Deserialize(new StringReader(xml));
            if (result is WebDescribeResponse describe)
            {
                var fromXml = ReadXmlInt64(xml, "deathCurrency");
                if (fromXml.HasValue)
                    describe.deathCurrency = fromXml.Value;
            }
        }
        catch (Exception e)
        {
            resultCallback(new Response<T>(e));
            return;
        }

        resultCallback(new Response<T>(result));
    }

    private static long? ReadXmlInt64(string xml, string elementName)
    {
        if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(elementName))
            return null;
        var match = Regex.Match(xml, $"<{elementName}>(-?\\d+)</{elementName}>");
        if (!match.Success)
            return null;
        if (!long.TryParse(match.Groups[1].Value, out var value))
            return null;
        return value;
    }

    private static string GetConfiguredWebServerUrl()
    {
        var configured = Environment.GetEnvironmentVariable(Web_Server_Url_Env);
        if (!string.IsNullOrWhiteSpace(configured))
            return NormalizeWebServerUrl(configured, false);

        configured = Environment.GetEnvironmentVariable(Local_Server_Host_Env);
        if (!string.IsNullOrWhiteSpace(configured))
            return NormalizeWebServerUrl(configured, true);

        configured = ReadLocalServerConfig();
        if (!string.IsNullOrWhiteSpace(configured))
            return NormalizeWebServerUrl(configured, true);

        return Local_Web_Server_Url;
    }

    private static string ReadLocalServerConfig()
    {
        foreach (var path in GetLocalServerConfigPaths())
        {
            try
            {
                if (!File.Exists(path)) continue;

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var value = ParseLocalServerConfigLine(rawLine);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read {path}: {e.Message}");
            }
        }

        return null;
    }

    private static string ParseLocalServerConfigLine(string rawLine)
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line)) return null;
        if (line.StartsWith("#")) return null;

        var separatorIndex = GetConfigSeparatorIndex(line);
        if (separatorIndex <= 0) return line;

        var key = line.Substring(0, separatorIndex).Trim();
        if (!key.Equals("ip", StringComparison.OrdinalIgnoreCase))
            return line.Contains("://") ? line : null;

        return line.Substring(separatorIndex + 1).Trim();
    }

    private static int GetConfigSeparatorIndex(string line)
    {
        var colonIndex = line.IndexOf(':');
        var equalsIndex = line.IndexOf('=');

        if (colonIndex < 0) return equalsIndex;
        if (equalsIndex < 0) return colonIndex;
        return Math.Min(colonIndex, equalsIndex);
    }

    private static IEnumerable<string> GetLocalServerConfigPaths()
    {
        var paths = new List<string>();

        try
        {
            paths.Add(Path.Combine(Directory.GetCurrentDirectory(), Local_Server_Config_File));
        }
        catch { }

        try
        {
            paths.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", Local_Server_Config_File)));
            paths.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Local_Server_Config_File)));
        }
        catch { }

        return paths.Distinct();
    }

    private static string NormalizeWebServerUrl(string value, bool useLocalDefaultPort)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Local_Web_Server_Url;

        value = value.Trim();
        if (!value.Contains("://"))
            value = "http://" + value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return Local_Web_Server_Url;

        var builder = new UriBuilder(uri);
        if (useLocalDefaultPort && uri.IsDefaultPort)
            builder.Port = 8443;

        var result = builder.Uri.ToString();
        if (!result.EndsWith("/"))
            result += "/";
        return result;
    }

    public static void SendForgotPassword(string email, Action<Response<WebLoginResponse>> callback)
    {
        SendRequest("v1/account/forgot", new Dictionary<string, string>()
        {
            { "email", Client.RsaEncrypt(email) },
        }, callback);
    }

    public static void SendWebLogin(string email, string hash, Action<Response<WebLoginResponse>> callback)
    {
        SendRequest("v1/account/login", new Dictionary<string, string>()
        {
            { "email", Client.RsaEncrypt(email) },
            { "hash", Client.RsaEncrypt(hash) }
        }, callback);
    }

    public static void SendWebRegister(string email, string hash, Action<Response<WebRegisterResponse>> callback)
    {
        SendRequest("v1/account/register", new Dictionary<string, string>()
        {
            { "email", Client.RsaEncrypt(email) },
            { "hash", Client.RsaEncrypt(hash) }
        }, callback);
    }

    public static void SendWebDescribe(string accessToken, Action<Response<WebDescribeResponse>> callback)
    {
        SendRequest("v1/account/describe", new Dictionary<string, string>()
        {
            { "token", Client.RsaEncrypt(accessToken) },
            { "version", NetConstants.Build_Version },
        }, callback);
    }

    public static void SendPurchaseSlot(string accessToken, Action<Response<WebPurchaseSlotResponse>> callback)
    {
        SendRequest("v1/account/purchaseslot", new Dictionary<string, string>()
        {
            { "token", Client.RsaEncrypt(accessToken) },
        }, callback);
    }

    public static void SendChangeName(string accessToken, string fromName, string toName, string reservation, Action<Response<WebNameChangeResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "token", Client.RsaEncrypt(accessToken) },
            { "fromName", fromName },
            { "toName", toName }
        };
        if (!string.IsNullOrEmpty(reservation))
            dict.Add("reservation", Client.RsaEncrypt(reservation));

        SendRequest("v1/account/changename", dict, callback);
    }

    public static void SendLeaderboardDescribe(LeaderboardType type, Action<Response<WebLeaderboardResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "type", ((int)type).ToString() }
        };

        SendRequest("v1/leaderboard/describe", dict, callback);
    }

    public static void SendServerList(Action<Response<WebServerListResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "version", NetConstants.Build_Version },
        };

        SendRequest("v1/server/list", dict, callback);
    }

    public static void SendDiscordPurchaseVerify(string id, Action<Response<WebVerifyResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "id", id },
            { "accountId", Account.describe.accountId.ToString() }
        };

        SendRequest("v1/purchase/discord/verify", dict, callback);
    }

    public static void SendiOSPurchaseVerify(string receipt, Action<Response<WebVerifyResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "receipt", receipt },
            { "accountId", Account.describe.accountId.ToString() }
        };

        SendRequest("v1/purchase/ios/verify", dict, callback);
    }

    public static void SendAndroidPurchaseVerify(string token, string productId, Action<Response<WebVerifyResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "token", token },
            { "productId", productId },
            { "accountId", Account.describe.accountId.ToString() }
        };

        SendRequest("v1/purchase/android/verify", dict, callback);
    }

    public static void SendSteamPurchaseStart(string steamId, string languageCode, string productId, Action<Response<WebSteamInitTxnResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "steamId", steamId },
            { "lan", languageCode },
            { "productId", productId },
            { "accountId", Account.describe.accountId.ToString() }
        };

        SendRequest("v1/purchase/steam/start", dict, callback);
    }

    public static void SendSteamPurchaseVerify(string orderId, Action<Response<WebVerifyResponse>> callback)
    {
        var dict = new Dictionary<string, string>()
        {
            { "orderId", orderId },
            { "accountId", Account.describe.accountId.ToString() }
        };

        SendRequest("v1/purchase/steam/verify", dict, callback);
    }

    public static void SendLocalFreePurchase(string productId, Action<Response<WebVerifyResponse>> callback)
    {
        var token = Account.loggedInAccessToken;
        if (string.IsNullOrWhiteSpace(token))
            token = Account.savedAccessToken;

        var dict = new Dictionary<string, string>()
        {
            { "productId", productId },
            { "token", Client.RsaEncrypt(token) }
        };

        SendRequest("v1/purchase/local/free", dict, callback);
    }
}
