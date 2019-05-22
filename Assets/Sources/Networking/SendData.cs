using System;
using ENet;

public struct SendData
{
    public Peer   Peer;
    public IntPtr Data;
    public int    Length;
}