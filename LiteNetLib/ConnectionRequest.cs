﻿using System.Net;
using System.Threading;
using LiteNetLib.Utils;

namespace LiteNetLib
{
    public enum ConnectionRequestResult
    {
        None,
        Accept,
        Reject
    }

    public enum ConnectionRequestType
    {
        Incoming,
        PeerToPeer
    }

    internal interface IConnectionRequestListener
    {
        void OnConnectionSolved(ConnectionRequest request, byte[] rejectData, int start, int length);
    }

    public class ConnectionRequest
    {
        private readonly IConnectionRequestListener _listener;
        private int _used;

        public IPEndPoint RemoteEndPoint { get { return Peer.EndPoint; } }
        //public readonly NetDataReader Data;
        public ConnectionRequestResult Result { get; private set; }
        public ConnectionRequestType Type { get; private set; }

        internal readonly long ConnectionId;
        internal readonly byte ConnectionNumber;
        internal readonly NetPeer Peer;
        public readonly byte[] Data;

        private bool TryActivate()
        {
            return Interlocked.CompareExchange(ref _used, 1, 0) == 0;
        }

        internal ConnectionRequest(
            long connectionId,
            byte connectionNumber,
            ConnectionRequestType type,
            NetDataReader netDataReader,
            NetPeer peer,
            IConnectionRequestListener listener)
        {
            ConnectionId = connectionId;
            ConnectionNumber = connectionNumber;
            Type = type;
            Peer = peer;
           // var dd = netDataReader.GetString();
            Data = netDataReader.GetRemainingBytes();
            _listener = listener;
        }

        /// <summary>
        /// Accept connection and get new NetPeer as result
        /// </summary>
        /// <returns>Connected NetPeer</returns>
        public NetPeer Accept()
        {
            if (!TryActivate())
                return null;
            Result = ConnectionRequestResult.Accept;
            _listener.OnConnectionSolved(this, null, 0, 0);
            return Peer;
        }

        public void Reject(byte[] rejectData, int start, int length)
        {
            if (!TryActivate())
                return;
            Result = ConnectionRequestResult.Reject;
            _listener.OnConnectionSolved(this, rejectData, start, length);
        }

        public void Reject()
        {
            Reject(null, 0, 0);
        }

        public void Reject(byte[] rejectData)
        {
            Reject(rejectData, 0, rejectData.Length);
        }

        public void Reject(NetDataWriter rejectData)
        {
            Reject(rejectData.Data, 0, rejectData.Length);
        }
    }
}
