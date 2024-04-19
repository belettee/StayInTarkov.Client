using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using StayInTarkov.Coop.Components.CoopGameComponents;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.NetworkPacket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using Mono.Cecil;
using StayInTarkov.Coop.SITGameModes;
using StayInTarkov.Coop.NetworkPacket.Player;
using UnityEngine.Networking;
using System.Threading;
using UnityEngine;

namespace StayInTarkov.Networking
{
    public static class SITGameServerClientDataProcessing
    {
        public static event Action<ushort> OnLatencyUpdated;

        public static ManualLogSource Logger { get; set; }

        static SITGameServerClientDataProcessing()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource($"{nameof(SITGameServerClientDataProcessing)}");
        }

        public static void ProcessPacketBytes(SITGameComponent gameComp, byte[] data)
        {
            try
            {
                if (data == null)
                {
                    Logger.LogError($"{nameof(ProcessPacketBytes)}. Data is Null");
                    return;
                }

                if (data.Length == 0)
                {
                    Logger.LogError($"{nameof(ProcessPacketBytes)}. Data is Empty");
                    return;
                }

                if (gameComp == null)
                    return;

                ISITPacket sitPacket = null;
                ProcessSITPacket(gameComp, data, out sitPacket);

                if (sitPacket != null)
                    gameComp.ActionPacketHandler.ActionSITPackets.Add(sitPacket);
                else
                {
#if DEBUG
                    Logger.LogDebug($">> DEV TODO <<");
                    Logger.LogDebug($">> Convert the following packet to binary <<");
                    Logger.LogDebug($"{Encoding.UTF8.GetString(data)}");
#endif 
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public static void ProcessSITPacket(SITGameComponent gameComp, byte[] data, out ISITPacket packet)
        {
            packet = null;

            // If the data is empty. Return;
            if (data == null || data.Length == 0)
            {
                Logger.LogError($"{nameof(ProcessSITPacket)}. {nameof(data)} is null");
            }

            var stringData = Encoding.UTF8.GetString(data);
            // If the string Data isn't a SIT serialized string. Return;
            if (!stringData.StartsWith("SIT"))
            {
                //Logger.LogError($"{nameof(ProcessSITPacket)}. {stringData} does not start with SIT");
                return;
            }

            var serverId = stringData.Substring(3, 24);
            // If the serverId is not the same as the one we are connected to. Return;
            if (serverId != gameComp.ServerId)
            {
                Logger.LogError($"{nameof(ProcessSITPacket)}. {serverId} does not equal {gameComp.ServerId}");
                return;
            }

            var bp = new BasePacket("");
            using (var br = new BinaryReader(new MemoryStream(data)))
                bp.ReadHeader(br);

            packet = DeserializeIntoPacket(data, packet, bp);
        }

        private static ISITPacket DeserializeIntoPacket(byte[] data, ISITPacket packet, BasePacket bp)
        {
            var sitPacketType =
                            StayInTarkovHelperConstants
                            .SITTypes
                            .Union(ReflectionHelpers.EftTypes)
                            .FirstOrDefault(x => x.Name == bp.Method);
            if (sitPacketType != null)
            {
                //Logger.LogInfo($"{sitPacketType} found");
                packet = (ISITPacket)Activator.CreateInstance(sitPacketType);
                packet = packet.Deserialize(data);
            }
            else
            {
#if DEBUG
                Logger.LogDebug($"{nameof(DeserializeIntoPacket)}:{bp.Method} could not find a matching ISITPacket type");
#endif
            }

            return packet;
        }

    }
}
