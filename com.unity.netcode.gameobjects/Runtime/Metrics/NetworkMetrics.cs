#if MULTIPLAYER_TOOLS
using System;
using System.Collections.Generic;
using Unity.Multiplayer.Tools;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Netcode
{
    internal class NetworkMetrics : INetworkMetrics
    {
        private static Dictionary<uint, string> s_SceneEventTypeNames;

        static NetworkMetrics()
        {
            s_SceneEventTypeNames = new Dictionary<uint, string>();
            foreach (SceneEventType type in Enum.GetValues(typeof(SceneEventType)))
            {
                s_SceneEventTypeNames[(uint)type] = type.ToString();
            }
        }

        private static string GetSceneEventTypeName(uint typeCode)
        {
            if (!s_SceneEventTypeNames.TryGetValue(typeCode, out string name))
            {
                name = "Unknown";
            }

            return name;
        }

        private readonly Counter m_TransportBytesSent = new Counter(NetworkMetricTypes.TotalBytesSent.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Counter m_TransportBytesReceived = new Counter(NetworkMetricTypes.TotalBytesReceived.Id)
        {
            ShouldResetOnDispatch = true,
        };

        private readonly EventMetric<NetworkMessageEvent> m_NetworkMessageSentEvent = new EventMetric<NetworkMessageEvent>(NetworkMetricTypes.NetworkMessageSent.Id);
        private readonly EventMetric<NetworkMessageEvent> m_NetworkMessageReceivedEvent = new EventMetric<NetworkMessageEvent>(NetworkMetricTypes.NetworkMessageReceived.Id);
        private readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(NetworkMetricTypes.NamedMessageSent.Id);
        private readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(NetworkMetricTypes.NamedMessageReceived.Id);
        private readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(NetworkMetricTypes.UnnamedMessageSent.Id);
        private readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(NetworkMetricTypes.UnnamedMessageReceived.Id);
        private readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaSentEvent = new EventMetric<NetworkVariableEvent>(NetworkMetricTypes.NetworkVariableDeltaSent.Id);
        private readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaReceivedEvent = new EventMetric<NetworkVariableEvent>(NetworkMetricTypes.NetworkVariableDeltaReceived.Id);
        private readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeSentEvent = new EventMetric<OwnershipChangeEvent>(NetworkMetricTypes.OwnershipChangeSent.Id);
        private readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeReceivedEvent = new EventMetric<OwnershipChangeEvent>(NetworkMetricTypes.OwnershipChangeReceived.Id);
        private readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnSentEvent = new EventMetric<ObjectSpawnedEvent>(NetworkMetricTypes.ObjectSpawnedSent.Id);
        private readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnReceivedEvent = new EventMetric<ObjectSpawnedEvent>(NetworkMetricTypes.ObjectSpawnedReceived.Id);
        private readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroySentEvent = new EventMetric<ObjectDestroyedEvent>(NetworkMetricTypes.ObjectDestroyedSent.Id);
        private readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroyReceivedEvent = new EventMetric<ObjectDestroyedEvent>(NetworkMetricTypes.ObjectDestroyedReceived.Id);
        private readonly EventMetric<RpcEvent> m_RpcSentEvent = new EventMetric<RpcEvent>(NetworkMetricTypes.RpcSent.Id);
        private readonly EventMetric<RpcEvent> m_RpcReceivedEvent = new EventMetric<RpcEvent>(NetworkMetricTypes.RpcReceived.Id);
        private readonly EventMetric<ServerLogEvent> m_ServerLogSentEvent = new EventMetric<ServerLogEvent>(NetworkMetricTypes.ServerLogSent.Id);
        private readonly EventMetric<ServerLogEvent> m_ServerLogReceivedEvent = new EventMetric<ServerLogEvent>(NetworkMetricTypes.ServerLogReceived.Id);
        private readonly EventMetric<SceneEventMetric> m_SceneEventSentEvent = new EventMetric<SceneEventMetric>(NetworkMetricTypes.SceneEventSent.Id);
        private readonly EventMetric<SceneEventMetric> m_SceneEventReceivedEvent = new EventMetric<SceneEventMetric>(NetworkMetricTypes.SceneEventReceived.Id);
        private bool m_Dirty;

        public NetworkMetrics()
        {
            Dispatcher = new MetricDispatcherBuilder()
                .WithCounters(m_TransportBytesSent, m_TransportBytesReceived)
                .WithMetricEvents(m_NetworkMessageSentEvent, m_NetworkMessageReceivedEvent)
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .WithMetricEvents(m_NetworkVariableDeltaSentEvent, m_NetworkVariableDeltaReceivedEvent)
                .WithMetricEvents(m_OwnershipChangeSentEvent, m_OwnershipChangeReceivedEvent)
                .WithMetricEvents(m_ObjectSpawnSentEvent, m_ObjectSpawnReceivedEvent)
                .WithMetricEvents(m_ObjectDestroySentEvent, m_ObjectDestroyReceivedEvent)
                .WithMetricEvents(m_RpcSentEvent, m_RpcReceivedEvent)
                .WithMetricEvents(m_ServerLogSentEvent, m_ServerLogReceivedEvent)
                .WithMetricEvents(m_SceneEventSentEvent, m_SceneEventReceivedEvent)
                .Build();

            Dispatcher.RegisterObserver(NetcodeObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        public void SetConnectionId(ulong connectionId)
        {
            Dispatcher.SetConnectionId(connectionId);
        }

        public void TrackTransportBytesSent(long bytesCount)
        {
            m_TransportBytesSent.Increment(bytesCount);
        }

        public void TrackTransportBytesReceived(long bytesCount)
        {
            m_TransportBytesReceived.Increment(bytesCount);
        }

        public void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount)
        {
            m_NetworkMessageSentEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(receivedClientId), messageType, bytesCount));
            MarkDirty();
        }

        public void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount)
        {
            m_NetworkMessageReceivedEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(senderClientId), messageType, bytesCount));
            MarkDirty();
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
            MarkDirty();
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));
            MarkDirty();
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));
            MarkDirty();
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackUnnamedMessageSent(receiverClientId, bytesCount);
            }
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));
            MarkDirty();
        }

        public void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_NetworkVariableDeltaSentEvent.Mark(
                new NetworkVariableEvent(
                    new ConnectionInfo(receiverClientId),
                    GetObjectIdentifier(networkObject),
                    variableName,
                    networkBehaviourName,
                    bytesCount));
            MarkDirty();
        }

        public void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_NetworkVariableDeltaReceivedEvent.Mark(
                new NetworkVariableEvent(
                    new ConnectionInfo(senderClientId),
                    GetObjectIdentifier(networkObject),
                    variableName,
                    networkBehaviourName,
                    bytesCount));
            MarkDirty();
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            m_OwnershipChangeSentEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
            MarkDirty();
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            m_OwnershipChangeReceivedEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(senderClientId),
                GetObjectIdentifier(networkObject), bytesCount));
            MarkDirty();
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            m_ObjectSpawnSentEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
            MarkDirty();
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            m_ObjectSpawnReceivedEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));
            MarkDirty();
        }

        public void TrackObjectDestroySent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            m_ObjectDestroySentEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
            MarkDirty();
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            m_ObjectDestroyReceivedEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));
            MarkDirty();
        }

        public void TrackRpcSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_RpcSentEvent.Mark(
                new RpcEvent(
                    new ConnectionInfo(receiverClientId),
                    GetObjectIdentifier(networkObject),
                    rpcName,
                    networkBehaviourName,
                    bytesCount));
            MarkDirty();
        }

        public void TrackRpcSent(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObject, rpcName, networkBehaviourName, bytesCount);
            }
        }

        public void TrackRpcReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_RpcReceivedEvent.Mark(
                new RpcEvent(new ConnectionInfo(senderClientId),
                    GetObjectIdentifier(networkObject),
                    rpcName,
                    networkBehaviourName,
                    bytesCount));
            MarkDirty();
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            m_ServerLogSentEvent.Mark(new ServerLogEvent(new ConnectionInfo(receiverClientId), (Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
            MarkDirty();
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            m_ServerLogReceivedEvent.Mark(new ServerLogEvent(new ConnectionInfo(senderClientId), (Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
            MarkDirty();
        }

        public void TrackSceneEventSent(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType, string sceneName, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackSceneEventSent(receiverClientId, sceneEventType, sceneName, bytesCount);
            }
        }

        public void TrackSceneEventSent(ulong receiverClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            m_SceneEventSentEvent.Mark(new SceneEventMetric(new ConnectionInfo(receiverClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
            MarkDirty();
        }

        public void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            m_SceneEventReceivedEvent.Mark(new SceneEventMetric(new ConnectionInfo(senderClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
            MarkDirty();
        }

        public void DispatchFrame()
        {
            if (m_Dirty)
            {
                Dispatcher.Dispatch();
                m_Dirty = false;
            }
        }

        private void MarkDirty()
        {
            m_Dirty = true;
        }

        private static NetworkObjectIdentifier GetObjectIdentifier(NetworkObject networkObject)
        {
            return new NetworkObjectIdentifier(networkObject.GetNameForMetrics(), networkObject.NetworkObjectId);
        }
    }

    internal class NetcodeObserver
    {
        public static IMetricObserver Observer { get; } = MetricObserverFactory.Construct();
    }
}
#endif
