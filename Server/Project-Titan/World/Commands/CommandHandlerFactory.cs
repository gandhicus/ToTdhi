using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TitanCore.Core;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class CommandHandlerFactory
    {
        public static IEnumerable<CommandHandler> AllHandlers => handlers.Values;

        public static bool TryGetHandler(string name, out CommandHandler handler) => handlers.TryGetValue(name, out handler);

        private static Dictionary<string, CommandHandler> handlers;

        static CommandHandlerFactory()
        {
            var baseType = typeof(CommandHandler);
            handlers = baseType.Assembly.GetTypes().Where(_ => _.IsSubclassOf(baseType)).Select(_ => (CommandHandler)Activator.CreateInstance(_)).ToDictionary(_ => _.Command.ToLower());
        }

        public static ChatData Handle(Player player, CommandArgs args)
        {
            if (!handlers.TryGetValue(args.command, out var handler))
                return ChatData.Error($"Command '{args.command}' does not exist");

            if (!HasPermission(player, handler))
                return ChatData.Error("You do not have permission to use this command");

            return handler.Handle(player, args);
        }

        private static bool HasPermission(Player player, CommandHandler handler)
        {
            if ((Rank)player.rank.Value >= handler.MinRank)
                return true;

            if (!global::World.ServerSettings.AdminCommands || handler.MinRank > Rank.Admin)
                return false;

            return IsLocalClient(player);
        }

        private static bool IsLocalClient(Player player)
        {
            try
            {
                var address = player.client.RemoteAddress;
                return IPAddress.IsLoopback(address) ||
                    IPAddress.IsLoopback(address.MapToIPv4()) ||
                    IPAddress.IsLoopback(address.MapToIPv6());
            }
            catch
            {
                return false;
            }
        }
    }
}
