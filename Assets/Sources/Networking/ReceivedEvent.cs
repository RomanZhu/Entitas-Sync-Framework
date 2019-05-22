using System;
using ENet;

public struct ReceivedEvent
{
    public EventType EventType;
    public Peer      Peer;
    public IntPtr    Data;
}