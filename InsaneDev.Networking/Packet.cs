#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace InsaneDev.Networking
{
    public class Packet
    {
        public static readonly byte[] PacketStart = new byte[] {0, 48, 21, 0};
        public int DataPos;
        public bool Disposed = false;
        public Int16 ParamCount = 0;
        private byte[] _ReturnByteArray;
        public Int16 Type;
        private byte[] _Data = new byte[128];
        private List<object> _PacketObjects;

        public Packet(Int16 type)
        {
            Type = type;
        }

        public void Dispose()
        {
            _ReturnByteArray = null;
            if (Disposed) return;
            Disposed = true;
            if (_PacketObjects != null)
            {
                _PacketObjects.Clear();
                _PacketObjects = null;
            }
            _Data = null;
        }

        public Packet Copy()
        {
            Packet p = new Packet(Type) {_Data = new byte[_Data.Length]};
            _Data.CopyTo(p._Data, 0);
            p.DataPos = DataPos;
            if (_PacketObjects != null) p._PacketObjects = new List<object>(_PacketObjects);
            p.ParamCount = ParamCount;
            p._ReturnByteArray = _ReturnByteArray;
            return p;
        }

        public void AddDouble(Double d)
        {
            _ReturnByteArray = null;
            while (DataPos + 9 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Double;
            BitConverter.GetBytes(d).CopyTo(_Data, DataPos);
            DataPos += 8;
            ParamCount++;
        }

        public void AddBytePacket(byte[] byteArray)
        {
            _ReturnByteArray = null;
            int size = byteArray.Length;
            while (DataPos + (size + 5) >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.BytePacket;
            BitConverter.GetBytes(byteArray.Length).CopyTo(_Data, DataPos);
            DataPos += 4;
            byteArray.CopyTo(_Data, DataPos);
            DataPos += size;
            ParamCount++;
        }

        public void AddFloat(float f)
        {
            _ReturnByteArray = null;
            while (DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Float;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 4;
            ParamCount++;
        }

        public void AddBool(bool f)
        {
            _ReturnByteArray = null;
            while (DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Bool;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 1;
            ParamCount++;
        }

        public void AddLong(long f)
        {
            _ReturnByteArray = null;
            while (DataPos + 9 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Long;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 8;
            ParamCount++;
        }

        public void AddInt(Int32 f)
        {
            _ReturnByteArray = null;
            while (DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Int32;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 4;
            ParamCount++;
        }

        public void AddULong(UInt64 f)
        {
            _ReturnByteArray = null;
            while (DataPos + 9 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Ulong;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 8;
            ParamCount++;
        }

        public void AddShort(Int16 f)
        {
            _ReturnByteArray = null;
            while (DataPos + 3 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Short;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 2;
            ParamCount++;
        }

        public void AddUInt(UInt32 f)
        {
            _ReturnByteArray = null;
            while (DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[DataPos++] = (byte) ParamTypes.Uint32;
            BitConverter.GetBytes(f).CopyTo(_Data, DataPos);
            DataPos += 4;
            ParamCount++;
        }

        public byte[] ToByteArray()
        {
            if (_ReturnByteArray != null) return _ReturnByteArray;
            _ReturnByteArray = new byte[12 + DataPos];
            PacketStart.CopyTo(_ReturnByteArray, 0);
            BitConverter.GetBytes(ParamCount).CopyTo(_ReturnByteArray, 4);
            BitConverter.GetBytes(12 + DataPos).CopyTo(_ReturnByteArray, 6);
            BitConverter.GetBytes(Type).CopyTo(_ReturnByteArray, 10);
            Array.Copy(_Data, 0, _ReturnByteArray, 12, DataPos);
            return _ReturnByteArray;
        }

        public static Packet FromByteArray(byte[] data)
        {
            Packet returnPacket = new Packet(BitConverter.ToInt16(data, 10))
                {
                    ParamCount = BitConverter.ToInt16(data, 4),
                    _Data = new byte[BitConverter.ToInt32(data, 6) - 12]
                };
            returnPacket.DataPos = returnPacket._Data.Length;
            Array.Copy(data, 12, returnPacket._Data, 0, returnPacket._Data.Length);
            returnPacket.UpdateObjects();
            return returnPacket;
        }

        public object[] GetObjects()
        {
            return _PacketObjects.ToArray();
        }

        private void UpdateObjects()
        {
            _PacketObjects = new List<Object>(ParamCount);
            int bytepos = 0;
            try
            {
                for (int x = 0; x < ParamCount; x++)
                {
                    switch ((ParamTypes) _Data[bytepos++])
                    {
                        case ParamTypes.Double:
                            _PacketObjects.Add(BitConverter.ToDouble(_Data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.Float:
                            _PacketObjects.Add(BitConverter.ToSingle(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Int32:
                            _PacketObjects.Add(BitConverter.ToInt32(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Bool:
                            _PacketObjects.Add(BitConverter.ToBoolean(_Data, bytepos));
                            bytepos += 1;
                            break;
                        case ParamTypes.Long:
                            _PacketObjects.Add(BitConverter.ToInt64(_Data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.BytePacket:
                            byte[] data = new byte[BitConverter.ToInt32(_Data, bytepos)];
                            bytepos += 4;
                            Array.Copy(_Data, bytepos, data, 0, data.Length);
                            _PacketObjects.Add(data);
                            bytepos += data.Length;
                            break;
                        case ParamTypes.Uint32:
                            _PacketObjects.Add(BitConverter.ToUInt32(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Ulong:
                            _PacketObjects.Add(BitConverter.ToUInt64(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Short:
                            _PacketObjects.Add(BitConverter.ToInt16(_Data, bytepos));
                            bytepos += 2;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ExpandDataArray()
        {
            _ReturnByteArray = null;
            byte[] newData = new byte[_Data.Length*2];
            _Data.CopyTo(newData, 0);
            _Data = newData;
        }

        private enum ParamTypes
        {
            Double,
            Float,
            Int32,
            Bool,
            BytePacket,
            Long,
            Uint32,
            Ulong,
            Short
        };
    }
}