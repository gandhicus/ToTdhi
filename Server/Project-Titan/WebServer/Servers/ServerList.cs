using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TitanCore.Net.Web;
using Utils.NET.Modules;

namespace WebServer.Servers
{
    public class ServerList
    {
        private struct ServerInfo
        {
            public WebServerInfo webInfo;

            public DateTime lastUpdated;
        }

        private ConcurrentDictionary<string, ServerInfo> servers = new ConcurrentDictionary<string, ServerInfo>();

        public WebServerInfo[] infos = new WebServerInfo[0];

        private bool local;

        private WebServerInfo[] localLoopbackInfos = new WebServerInfo[0];

        public ServerList()
        {
            local = ModularProgram.manifest.Value("local", false);
            if (!local) return;

            var localHost = GetLocalServerHost();
            infos = new WebServerInfo[]
            {
                new WebServerInfo("Local", localHost, localHost, ServerStatus.Normal)
            };
            localLoopbackInfos = new WebServerInfo[]
            {
                new WebServerInfo("Local", "127.0.0.1", "127.0.0.1", ServerStatus.Normal)
            };
        }

        public WebServerInfo[] GetInfos(IPAddress requesterAddress)
        {
            if (local && IsLoopback(requesterAddress))
                return localLoopbackInfos;

            return infos;
        }

        private static string GetLocalServerHost()
        {
            var host = Environment.GetEnvironmentVariable("TRIALS_LOCAL_SERVER_HOST");
            if (string.IsNullOrWhiteSpace(host))
                host = ReadServerSetting("ip");
            if (string.IsNullOrWhiteSpace(host))
                host = ModularProgram.manifest.Value("localServerHost", "127.0.0.1");

            return NormalizeHost(host);
        }

        private static string NormalizeHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return "127.0.0.1";

            host = host.Trim();
            host = ReadInlineSetting(host, "ip") ?? host;

            var uriValue = host.Contains("://") ? host : "http://" + host;
            if (Uri.TryCreate(uriValue, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
                return uri.Host;

            var slashIndex = host.IndexOf('/');
            if (slashIndex >= 0)
                host = host.Substring(0, slashIndex);

            var colonIndex = host.LastIndexOf(':');
            if (colonIndex > 0 && host.IndexOf(':') == colonIndex)
                host = host.Substring(0, colonIndex);

            return string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
        }

        private static bool IsLoopback(IPAddress address)
        {
            if (address == null)
                return false;

            return IPAddress.IsLoopback(address) ||
                IPAddress.IsLoopback(address.MapToIPv4()) ||
                IPAddress.IsLoopback(address.MapToIPv6());
        }

        private static string ReadServerSetting(string key)
        {
            foreach (var path in GetServerSettingPaths())
            {
                try
                {
                    if (!File.Exists(path)) continue;

                    foreach (var rawLine in File.ReadAllLines(path))
                    {
                        var value = ReadInlineSetting(rawLine, key);
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
                catch { }
            }

            return null;
        }

        private static IEnumerable<string> GetServerSettingPaths()
        {
            var fileName = "ServerSettings.txt";
            yield return Path.Combine(Directory.GetCurrentDirectory(), fileName);

            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            if (parent != null)
                yield return Path.Combine(parent.FullName, fileName);

            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
            if (parent != null)
                yield return Path.Combine(parent.FullName, fileName);
        }

        private static string ReadInlineSetting(string rawLine, string key)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                return null;

            var separatorIndex = GetSeparatorIndex(line);
            if (separatorIndex <= 0)
                return null;

            var lineKey = line.Substring(0, separatorIndex).Trim();
            if (!lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                return null;

            return line.Substring(separatorIndex + 1).Trim();
        }

        private static int GetSeparatorIndex(string line)
        {
            var colonIndex = line.IndexOf(':');
            var equalsIndex = line.IndexOf('=');

            if (colonIndex < 0)
                return equalsIndex;
            if (equalsIndex < 0)
                return colonIndex;
            return Math.Min(colonIndex, equalsIndex);
        }

        private void UpdateInfos()
        {
            var list = new List<WebServerInfo>();
            foreach (var info in servers.ToArray().Select(_ => _.Value))
            {
                if ((DateTime.Now - info.lastUpdated).TotalSeconds > 30)
                    servers.TryRemove(info.webInfo.name, out var v);
                else
                    list.Add(info.webInfo);
            }
            infos = list.OrderBy(_ => _.name).ToArray();
        }

        public void PushUpdate(string name, string host, string pingHost, ServerStatus status)
        {
            servers[name] = new ServerInfo()
            {
                webInfo = new WebServerInfo(name, host, pingHost, status),
                lastUpdated = DateTime.Now
            };
            UpdateInfos();
        }
    }
}
