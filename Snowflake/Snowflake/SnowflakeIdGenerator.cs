using System;

public class SnowflakeIdGenerator
{
    private const long Twepoch = 1577836800000L;
    private const int WorkerIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
    private const int SequenceBits = 12;
    private const int WorkerIdShift = SequenceBits;
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
    private const long SequenceMask = -1L ^ (-1L << SequenceBits);

    private static long _lastTimestamp = -1L;
    private static long _sequence = 0L;
    private static readonly object _lock = new object();

    public static long WorkerId { get; private set; }
    public static long DatacenterId { get; private set; }

    public static void Initialize(long workerId, long datacenterId)
    {
        if (workerId > MaxWorkerId || workerId < 0)
        {
            throw new ArgumentException($"worker Id can't be greater than {MaxWorkerId} or less than 0");
        }

        if (datacenterId > MaxDatacenterId || datacenterId < 0)
        {
            throw new ArgumentException($"datacenter Id can't be greater than {MaxDatacenterId} or less than 0");
        }

        WorkerId = workerId;
        DatacenterId = datacenterId;
    }

    public static long NextId()
    {
        lock (_lock)
        {
            long timestamp = CurrentTimeMillis();

            if (timestamp < _lastTimestamp)
            {
                throw new Exception($"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp} milliseconds");
            }

            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    timestamp = TillNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0L;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Twepoch) << TimestampLeftShift) |
                   (DatacenterId << DatacenterIdShift) |
                   (WorkerId << WorkerIdShift) |
                   _sequence;
        }
    }

    private static long TillNextMillis(long lastTimestamp)
    {
        long timestamp = CurrentTimeMillis();
        while (timestamp <= lastTimestamp)
        {
            timestamp = CurrentTimeMillis();
        }
        return timestamp;
    }

    private static long CurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
