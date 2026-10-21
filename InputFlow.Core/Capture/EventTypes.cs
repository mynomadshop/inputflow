using System;

namespace InputFlow.Core.Capture;

/// <summary>Raised when an initial key-down is observed.</summary>
public class KeyPressEvent : EventArgs
{
    public ushort UsagePage { get; }
    public ushort Usage { get; }
    public ulong DeviceHandle { get; }
    public DateTime OccurredUtc { get; }

    public KeyPressEvent(ushort usagePage, ushort usage, ulong deviceHandle)
    {
        UsagePage = usagePage;
        Usage = usage;
        DeviceHandle = deviceHandle;
        OccurredUtc = DateTime.UtcNow;
    }
}

/// <summary>Raised when the pointer moves.</summary>
public class PointerEventArgs : EventArgs
{
    public int X { get; }
    public int Y { get; }
    public ulong TimeStamp { get; }

    public PointerEventArgs(int x, int y, ulong timeStamp)
    {
        X = x;
        Y = y;
        TimeStamp = timeStamp;
    }
}
