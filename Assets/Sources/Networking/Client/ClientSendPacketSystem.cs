using System;
using System.Runtime.InteropServices;
using Entitas;

namespace Sources.Networking.Client
{
    public class ClientSendPacketSystem : IExecuteSystem
    {
        private readonly ClientNetworkSystem _client;

        private readonly byte[] _data = new byte[2048];

        public ClientSendPacketSystem(Services services)
        {
            _client = services.ClientSystem;
        }

        public unsafe void Execute()
        {
            if (_client.State != ClientState.Connected) return;

            var commandCount  = _client.EnqueuedCommandCount;

            if (commandCount == 0) return;
            
            var commandLength = _client.ToServer.Length;
            var totalLength   = commandLength + 4;

            fixed (byte* destination = &_data[0])
            {
                var shortsSpan = new Span<ushort>(destination, 2);
                shortsSpan[0] = commandCount;
                shortsSpan[1] = (ushort) commandLength;
            }

            if (commandCount > 0)
            {
                _client.ToServer.ToArray(_data, 4);

                _client.EnqueuedCommandCount = 0;
            }

            var newPtr = Marshal.AllocHGlobal(totalLength);
            fixed (byte* source = &_data[0])
            {
                Buffer.MemoryCopy(source, newPtr.ToPointer(), totalLength, totalLength);
            }

            _client.EnqueueSendData(new SendData
                {Data = newPtr, Length = totalLength, Peer = _client.ServerConnection});
            _client.ToServer.Clear();
        }
    }
}