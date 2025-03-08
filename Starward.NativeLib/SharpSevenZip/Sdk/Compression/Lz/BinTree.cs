using SharpSevenZip.Sdk.Common;

namespace SharpSevenZip.Sdk.Compression.Lz;

internal class BinTree : InWindow, IMatchFinder
{
    private const uint kBT2HashSize = 1 << 16;
    private const uint kEmptyHashValue = 0;
    private const uint kHash2Size = 1 << 10;
    private const uint kHash3Offset = kHash2Size;
    private const uint kHash3Size = 1 << 16;
    private const uint kMaxValForNormalize = ((uint)1 << 31) - 1;
    private const uint kStartMaxLen = 1;
    private uint _cutValue = 0xFF;
    private uint _cyclicBufferPos;
    private uint _cyclicBufferSize;
    private uint[]? _hash;

    private uint _hashMask;
    private uint _hashSizeSum;
    private uint _matchMaxLen;

    private uint[]? _son;

    private bool HASH_ARRAY = true;

    private uint kFixHashSize = kHash2Size + kHash3Size;
    private uint kMinMatchCheck = 4;
    private uint kNumHashDirectBytes;

    #region IMatchFinder Members

    public new void SetStream(Stream stream)
    {
        base.SetStream(stream);
    }

    public new void ReleaseStream()
    {
        base.ReleaseStream();
    }

    public new void Init()
    {
        base.Init();
        for (uint i = 0; i < _hashSizeSum; i++)
            _hash![i] = kEmptyHashValue;
        _cyclicBufferPos = 0;
        ReduceOffsets(-1);
    }

    public new byte GetIndexByte(int index)
    {
        return base.GetIndexByte(index);
    }

    public new uint GetMatchLen(int index, uint distance, uint limit)
    {
        return base.GetMatchLen(index, distance, limit);
    }

    public new uint GetNumAvailableBytes()
    {
        return base.GetNumAvailableBytes();
    }

    public void Create(uint historySize, uint keepAddBufferBefore,
                       uint matchMaxLen, uint keepAddBufferAfter)
    {
        if (historySize + 256 > kMaxValForNormalize)
        {
            throw new ArgumentException("historySize + 256 > kMaxValForNormalize", nameof(historySize));
        }
        _cutValue = 16 + (matchMaxLen >> 1);

        uint windowReservSize = (historySize + keepAddBufferBefore +
                                   matchMaxLen + keepAddBufferAfter) / 2 + 256;

        base.Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, windowReservSize);

        _matchMaxLen = matchMaxLen;

        uint cyclicBufferSize = historySize + 1;
        if (_cyclicBufferSize != cyclicBufferSize)
            _son = new uint[(_cyclicBufferSize = cyclicBufferSize) * 2];

        uint hs = kBT2HashSize;

        if (HASH_ARRAY)
        {
            hs = historySize - 1;
            hs |= (hs >> 1);
            hs |= (hs >> 2);
            hs |= (hs >> 4);
            hs |= (hs >> 8);
            hs >>= 1;
            hs |= 0xFFFF;
            if (hs > (1 << 24))
                hs >>= 1;
            _hashMask = hs;
            hs++;
            hs += kFixHashSize;
        }
        if (hs != _hashSizeSum)
            _hash = new uint[_hashSizeSum = hs];
    }

    public uint GetMatches(uint[] distances)
    {
        uint lenLimit;
        if (_pos + _matchMaxLen <= _streamPos)
            lenLimit = _matchMaxLen;
        else
        {
            lenLimit = _streamPos - _pos;
            if (lenLimit < kMinMatchCheck)
            {
                MovePos();
                return 0;
            }
        }

        uint offset = 0;
        uint matchMinPos = (_pos > _cyclicBufferSize) ? (_pos - _cyclicBufferSize) : 0;
        uint cur = _bufferOffset + _pos;
        uint maxLen = kStartMaxLen; // to avoid items for len < hashSize;
        uint hashValue, hash2Value = 0, hash3Value = 0;

        if (HASH_ARRAY)
        {
            uint temp = Crc.Table[_bufferBase![cur]] ^ _bufferBase[cur + 1];
            hash2Value = (temp & (((int)kHash2Size) - 1));
            temp ^= (uint)((_bufferBase[cur + 2]) << 8);
            hash3Value = (temp & (((int)kHash3Size) - 1));
            hashValue = (temp ^ (Crc.Table[_bufferBase[cur + 3]] << 5)) & _hashMask;
        }
        else
            hashValue = _bufferBase![cur] ^ ((uint)(_bufferBase[cur + 1]) << 8);

        uint curMatch = _hash![kFixHashSize + hashValue];
        if (HASH_ARRAY)
        {
            uint curMatch2 = _hash[hash2Value];
            uint curMatch3 = _hash[kHash3Offset + hash3Value];
            _hash[hash2Value] = _pos;
            _hash[kHash3Offset + hash3Value] = _pos;
            if (curMatch2 > matchMinPos)
                if (_bufferBase[_bufferOffset + curMatch2] == _bufferBase[cur])
                {
                    distances[offset++] = maxLen = 2;
                    distances[offset++] = _pos - curMatch2 - 1;
                }
            if (curMatch3 > matchMinPos)
                if (_bufferBase[_bufferOffset + curMatch3] == _bufferBase[cur])
                {
                    if (curMatch3 == curMatch2)
                        offset -= 2;
                    distances[offset++] = maxLen = 3;
                    distances[offset++] = _pos - curMatch3 - 1;
                    curMatch2 = curMatch3;
                }
            if (offset != 0 && curMatch2 == curMatch)
            {
                offset -= 2;
                maxLen = kStartMaxLen;
            }
        }

        _hash[kFixHashSize + hashValue] = _pos;

        uint ptr0 = (_cyclicBufferPos << 1) + 1;
        uint ptr1 = (_cyclicBufferPos << 1);

        uint len0, len1;
        len0 = len1 = kNumHashDirectBytes;

        if (kNumHashDirectBytes != 0)
        {
            if (curMatch > matchMinPos)
            {
                if (_bufferBase[_bufferOffset + curMatch + kNumHashDirectBytes] !=
                    _bufferBase[cur + kNumHashDirectBytes])
                {
                    distances[offset++] = maxLen = kNumHashDirectBytes;
                    distances[offset++] = _pos - curMatch - 1;
                }
            }
        }

        uint count = _cutValue;

        while (true)
        {
            if (curMatch <= matchMinPos || count-- == 0)
            {
                _son![ptr0] = _son[ptr1] = kEmptyHashValue;
                break;
            }
            uint delta = _pos - curMatch;
            uint cyclicPos = ((delta <= _cyclicBufferPos)
                                    ?
                                        (_cyclicBufferPos - delta)
                                    :
                                        (_cyclicBufferPos - delta + _cyclicBufferSize)) << 1;

            uint pby1 = _bufferOffset + curMatch;
            uint len = Math.Min(len0, len1);
            if (_bufferBase[pby1 + len] == _bufferBase[cur + len])
            {
                while (++len != lenLimit)
                    if (_bufferBase[pby1 + len] != _bufferBase[cur + len])
                        break;
                if (maxLen < len)
                {
                    distances[offset++] = maxLen = len;
                    distances[offset++] = delta - 1;
                    if (len == lenLimit)
                    {
                        _son![ptr1] = _son[cyclicPos];
                        _son[ptr0] = _son[cyclicPos + 1];
                        break;
                    }
                }
            }
            if (_bufferBase[pby1 + len] < _bufferBase[cur + len])
            {
                _son![ptr1] = curMatch;
                ptr1 = cyclicPos + 1;
                curMatch = _son[ptr1];
                len1 = len;
            }
            else
            {
                _son![ptr0] = curMatch;
                ptr0 = cyclicPos;
                curMatch = _son[ptr0];
                len0 = len;
            }
        }
        MovePos();
        return offset;
    }

    public void Skip(uint num)
    {
        do
        {
            uint lenLimit;
            if (_pos + _matchMaxLen <= _streamPos)
                lenLimit = _matchMaxLen;
            else
            {
                lenLimit = _streamPos - _pos;
                if (lenLimit < kMinMatchCheck)
                {
                    MovePos();
                    continue;
                }
            }

            uint matchMinPos = (_pos > _cyclicBufferSize) ? (_pos - _cyclicBufferSize) : 0;
            uint cur = _bufferOffset + _pos;

            uint hashValue;

            if (HASH_ARRAY)
            {
                uint temp = Crc.Table[_bufferBase![cur]] ^ _bufferBase[cur + 1];
                uint hash2Value = (temp & (((int)kHash2Size) - 1));
                _hash![hash2Value] = _pos;
                temp ^= ((uint)(_bufferBase[cur + 2]) << 8);
                uint hash3Value = (temp & (((int)kHash3Size) - 1));
                _hash[kHash3Offset + hash3Value] = _pos;
                hashValue = (temp ^ (Crc.Table[_bufferBase[cur + 3]] << 5)) & _hashMask;
            }
            else
                hashValue = _bufferBase![cur] ^ ((uint)(_bufferBase[cur + 1]) << 8);

            uint curMatch = _hash![kFixHashSize + hashValue];
            _hash[kFixHashSize + hashValue] = _pos;

            uint ptr0 = (_cyclicBufferPos << 1) + 1;
            uint ptr1 = (_cyclicBufferPos << 1);

            uint len0, len1;
            len0 = len1 = kNumHashDirectBytes;

            uint count = _cutValue;
            while (true)
            {
                if (curMatch <= matchMinPos || count-- == 0)
                {
                    _son![ptr0] = _son[ptr1] = kEmptyHashValue;
                    break;
                }

                uint delta = _pos - curMatch;
                uint cyclicPos = ((delta <= _cyclicBufferPos)
                                        ?
                                            (_cyclicBufferPos - delta)
                                        :
                                            (_cyclicBufferPos - delta + _cyclicBufferSize)) << 1;

                uint pby1 = _bufferOffset + curMatch;
                uint len = Math.Min(len0, len1);
                if (_bufferBase[pby1 + len] == _bufferBase[cur + len])
                {
                    while (++len != lenLimit)
                        if (_bufferBase[pby1 + len] != _bufferBase[cur + len])
                            break;
                    if (len == lenLimit)
                    {
                        _son![ptr1] = _son[cyclicPos];
                        _son[ptr0] = _son[cyclicPos + 1];
                        break;
                    }
                }
                if (_bufferBase[pby1 + len] < _bufferBase[cur + len])
                {
                    _son![ptr1] = curMatch;
                    ptr1 = cyclicPos + 1;
                    curMatch = _son[ptr1];
                    len1 = len;
                }
                else
                {
                    _son![ptr0] = curMatch;
                    ptr0 = cyclicPos;
                    curMatch = _son[ptr0];
                    len0 = len;
                }
            }
            MovePos();
        } while (--num != 0);
    }

    #endregion

    public void SetType(int numHashBytes)
    {
        HASH_ARRAY = (numHashBytes > 2);
        if (HASH_ARRAY)
        {
            kNumHashDirectBytes = 0;
            kMinMatchCheck = 4;
            kFixHashSize = kHash2Size + kHash3Size;
        }
        else
        {
            kNumHashDirectBytes = 2;
            kMinMatchCheck = 2 + 1;
            kFixHashSize = 0;
        }
    }

    public new void MovePos()
    {
        if (++_cyclicBufferPos >= _cyclicBufferSize)
            _cyclicBufferPos = 0;
        base.MovePos();
        if (_pos == kMaxValForNormalize)
            Normalize();
    }

    private static void NormalizeLinks(uint[] items, uint numItems, uint subValue)
    {
        for (uint i = 0; i < numItems; i++)
        {
            uint value = items[i];
            if (value <= subValue)
                value = kEmptyHashValue;
            else
                value -= subValue;
            items[i] = value;
        }
    }

    private void Normalize()
    {
        uint subValue = _pos - _cyclicBufferSize;
        NormalizeLinks(_son!, _cyclicBufferSize * 2, subValue);
        NormalizeLinks(_hash!, _hashSizeSum, subValue);
        ReduceOffsets((int)subValue);
    }

    //public void SetCutValue(UInt32 cutValue) { _cutValue = cutValue; }
}
