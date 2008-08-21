using System;
using System.Collections.Generic;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace Simian.Extensions
{
    class AvatarManager : ISimianExtension
    {
        Simian Server;
        int currentWearablesSerialNum = -1;

        public AvatarManager(Simian server)
        {
            Server = server;
        }

        public void Start()
        {
            Server.UDPServer.RegisterPacketCallback(PacketType.AvatarPropertiesRequest, new UDPServer.PacketCallback(AvatarPropertiesRequestHandler));
            Server.UDPServer.RegisterPacketCallback(PacketType.AgentWearablesRequest, new UDPServer.PacketCallback(AgentWearablesRequestHandler));
            Server.UDPServer.RegisterPacketCallback(PacketType.AgentIsNowWearing, new UDPServer.PacketCallback(AgentIsNowWearingHandler));
            Server.UDPServer.RegisterPacketCallback(PacketType.AgentSetAppearance, new UDPServer.PacketCallback(AgentSetAppearanceHandler));
        }

        public void Stop()
        {
        }

        void AvatarPropertiesRequestHandler(Packet packet, Agent agent)
        {
            AvatarPropertiesRequestPacket request = (AvatarPropertiesRequestPacket)packet;

            lock (Server.Agents)
            {
                foreach (Agent agt in Server.Agents.Values)
                {
                    if (agent.AgentID == request.AgentData.AvatarID)
                    {
                        AvatarPropertiesReplyPacket reply = new AvatarPropertiesReplyPacket();
                        reply.AgentData.AgentID = agt.AgentID;
                        reply.AgentData.AvatarID = request.AgentData.AvatarID;
                        reply.PropertiesData.AboutText = Utils.StringToBytes("Profile info unavailable");
                        reply.PropertiesData.BornOn = Utils.StringToBytes("Unknown");
                        reply.PropertiesData.CharterMember = Utils.StringToBytes("Test User");
                        reply.PropertiesData.FLAboutText = Utils.StringToBytes("First life info unavailable");
                        reply.PropertiesData.Flags = 0;
                        //TODO: at least generate static image uuids based on name.
                        //this will prevent re-caching the default image for the same av name.
                        reply.PropertiesData.FLImageID = agent.AgentID; //temporary hack
                        reply.PropertiesData.ImageID = agent.AgentID; //temporary hack
                        reply.PropertiesData.PartnerID = UUID.Zero;
                        reply.PropertiesData.ProfileURL = Utils.StringToBytes(String.Empty);

                        agent.SendPacket(reply);

                        break;
                    }
                }
            }
        }

        void AgentWearablesRequestHandler(Packet packet, Agent agent)
        {
            AgentWearablesUpdatePacket update = new AgentWearablesUpdatePacket();
            update.AgentData.AgentID = agent.AgentID;
            // Technically this should be per-agent, but if the only requirement is that it
            // increments this is easier
            update.AgentData.SerialNum = (uint)Interlocked.Increment(ref currentWearablesSerialNum);
            update.WearableData = new AgentWearablesUpdatePacket.WearableDataBlock[5];

            // TODO: These are hardcoded in for now, should change that
            update.WearableData[0] = new AgentWearablesUpdatePacket.WearableDataBlock();
            update.WearableData[0].AssetID = new UUID("dc675529-7ba5-4976-b91d-dcb9e5e36188");
            update.WearableData[0].ItemID = UUID.Random();
            update.WearableData[0].WearableType = (byte)WearableType.Hair;

            update.WearableData[1] = new AgentWearablesUpdatePacket.WearableDataBlock();
            update.WearableData[1].AssetID = new UUID("3e8ee2d6-4f21-4a55-832d-77daa505edff");
            update.WearableData[1].ItemID = UUID.Random();
            update.WearableData[1].WearableType = (byte)WearableType.Pants;

            update.WearableData[2] = new AgentWearablesUpdatePacket.WearableDataBlock();
            update.WearableData[2].AssetID = new UUID("530a2614-052e-49a2-af0e-534bb3c05af0");
            update.WearableData[2].ItemID = UUID.Random();
            update.WearableData[2].WearableType = (byte)WearableType.Shape;

            update.WearableData[3] = new AgentWearablesUpdatePacket.WearableDataBlock();
            update.WearableData[3].AssetID = new UUID("6a714f37-fe53-4230-b46f-8db384465981");
            update.WearableData[3].ItemID = UUID.Random();
            update.WearableData[3].WearableType = (byte)WearableType.Shirt;

            update.WearableData[4] = new AgentWearablesUpdatePacket.WearableDataBlock();
            update.WearableData[4].AssetID = new UUID("5f787f25-f761-4a35-9764-6418ee4774c4");
            update.WearableData[4].ItemID = UUID.Random();
            update.WearableData[4].WearableType = (byte)WearableType.Skin;

            agent.SendPacket(update);
        }

        void AgentIsNowWearingHandler(Packet packet, Agent agent)
        {
            AgentIsNowWearingPacket wearing = (AgentIsNowWearingPacket)packet;

            Logger.DebugLog("Updating agent wearables");

            lock (agent.Wearables)
            {
                agent.Wearables.Clear();

                for (int i = 0; i < wearing.WearableData.Length; i++)
                    agent.Wearables[(WearableType)wearing.WearableData[i].WearableType] = wearing.WearableData[i].ItemID;
            }
        }

        void AgentSetAppearanceHandler(Packet packet, Agent agent)
        {
            AgentSetAppearancePacket set = (AgentSetAppearancePacket)packet;

            agent.Avatar.Textures = new LLObject.TextureEntry(set.ObjectData.TextureEntry, 0,
                set.ObjectData.TextureEntry.Length);

            Logger.DebugLog("Updating avatar appearance");

            AvatarAppearancePacket appearance = new AvatarAppearancePacket();
            appearance.ObjectData.TextureEntry = set.ObjectData.TextureEntry;
            appearance.Sender.ID = agent.AgentID;
            appearance.Sender.IsTrial = false;

            // TODO: Store these visual params in Agent
            appearance.VisualParam = new AvatarAppearancePacket.VisualParamBlock[set.VisualParam.Length];
            for (int i = 0; i < set.VisualParam.Length; i++)
            {
                appearance.VisualParam[i] = new AvatarAppearancePacket.VisualParamBlock();
                appearance.VisualParam[i].ParamValue = set.VisualParam[i].ParamValue;
            }

            //TODO: What is WearableData used for?

            ObjectUpdatePacket update = Movement.BuildFullUpdate(agent, agent.Avatar, Server.RegionHandle,
                agent.State, agent.Flags | LLObject.ObjectFlags.ObjectYouOwner);
            agent.SendPacket(update);

            lock (Server.Agents)
            {
                foreach (Agent recipient in Server.Agents.Values)
                {
                    if (recipient != agent)
                        recipient.SendPacket(appearance);
                }
            }
        }
    }
}
