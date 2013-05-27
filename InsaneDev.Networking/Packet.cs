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

        private byte[] ReturnByteArray;
        public Int16 Type;
        private byte[] _data = new byte[128];
        private List<object> _packetObjects;

        public Packet(Int16 type)
        {
            Type = type;
        }

        public void Dispose()
        {
            ReturnByteArray = null;
            if (Disposed) return;
            Disposed = true;
            if (_packetObjects != null)
            {
                _packetObjects.Clear();
                _packetObjects = null;
            }
            _data = null;
        }

        public Packet Copy()
        {
            Packet p = new Packet(Type);
            p._data = new byte[_data.Length];
            _data.CopyTo(p._data, 0);
            p.DataPos = DataPos;
            if (_packetObjects != null) p._packetObjects = new List<object>(_packetObjects);
            p.ParamCount = ParamCount;
            p.ReturnByteArray = ReturnByteArray;
            return p;
        }

        public void AddDouble(Double d)
        {
            ReturnByteArray = null;
            while (DataPos + 9 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Double;
            BitConverter.GetBytes(d).CopyTo(_data, DataPos);
            DataPos += 8;
            ParamCount++;
        }

        public void AddBytePacket(byte[] byteArray)
        {
            ReturnByteArray = null;
            int size = byteArray.Length;
            while (DataPos + (size + 5) >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.BytePacket;
            BitConverter.GetBytes(byteArray.Length).CopyTo(_data, DataPos);
            DataPos += 4;
            byteArray.CopyTo(_data, DataPos);
            DataPos += size;
            ParamCount++;
        }

        public void AddFloat(float f)
        {
            ReturnByteArray = null;
            while (DataPos + 5 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Float;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 4;
            ParamCount++;
        }

        public void AddBool(bool f)
        {
            ReturnByteArray = null;
            while (DataPos + 5 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Bool;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 1;
            ParamCount++;
        }

        public void AddLong(long f)
        {
            ReturnByteArray = null;
            while (DataPos + 9 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Long;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 8;
            ParamCount++;
        }

        public void AddInt(Int32 f)
        {
            ReturnByteArray = null;
            while (DataPos + 5 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Int32;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 4;
            ParamCount++;
        }

        public void AddULong(UInt64 f)
        {
            ReturnByteArray = null;
            while (DataPos + 9 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Ulong;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 8;
            ParamCount++;
        }

        public void AddShort(Int16 f)
        {
            ReturnByteArray = null;
            while (DataPos + 3 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Short;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 2;
            ParamCount++;
        }

        public void AddUInt(UInt32 f)
        {
            ReturnByteArray = null;
            while (DataPos + 5 >= _data.Length) ExpandDataArray();
            _data[DataPos++] = (byte) ParamTypes.Uint32;
            BitConverter.GetBytes(f).CopyTo(_data, DataPos);
            DataPos += 4;
            ParamCount++;
        }

        public byte[] ToByteArray()
        {
            if (ReturnByteArray != null) return ReturnByteArray;
            ReturnByteArray = new byte[12 + DataPos];
            PacketStart.CopyTo(ReturnByteArray, 0);
            BitConverter.GetBytes(ParamCount).CopyTo(ReturnByteArray, 4);
            BitConverter.GetBytes(12 + DataPos).CopyTo(ReturnByteArray, 6);
            BitConverter.GetBytes(Type).CopyTo(ReturnByteArray, 10);
            Array.Copy(_data, 0, ReturnByteArray, 12, DataPos);
            return ReturnByteArray;
        }

        public static Packet FromByteArray(byte[] data)
        {
            Packet returnPacket = new Packet(BitConverter.ToInt16(data, 10))
                {
                    ParamCount = BitConverter.ToInt16(data, 4),
                    _data = new byte[BitConverter.ToInt32(data, 6) - 12]
                };
            returnPacket.DataPos = returnPacket._data.Length;
            Array.Copy(data, 12, returnPacket._data, 0, returnPacket._data.Length);
            returnPacket.UpdateObjects();
            return returnPacket;
        }

        public object[] GetObjects()
        {
            return _packetObjects.ToArray();
        }

        private void UpdateObjects()
        {
            _packetObjects = new List<Object>(ParamCount);
            int bytepos = 0;
            try
            {
                for (int x = 0; x < ParamCount; x++)
                {
                    switch ((ParamTypes) _data[bytepos++])
                    {
                        case ParamTypes.Double:
                            _packetObjects.Add(BitConverter.ToDouble(_data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.Float:
                            _packetObjects.Add(BitConverter.ToSingle(_data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Int32:
                            _packetObjects.Add(BitConverter.ToInt32(_data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Bool:
                            _packetObjects.Add(BitConverter.ToBoolean(_data, bytepos));
                            bytepos += 1;
                            break;
                        case ParamTypes.Long:
                            _packetObjects.Add(BitConverter.ToInt64(_data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.BytePacket:
                            byte[] data = new byte[BitConverter.ToInt32(_data, bytepos)];
                            bytepos += 4;
                            Array.Copy(_data, bytepos, data, 0, data.Length);
                            _packetObjects.Add(data);
                            bytepos += data.Length;
                            break;
                        case ParamTypes.Uint32:
                            _packetObjects.Add(BitConverter.ToUInt32(_data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Ulong:
                            _packetObjects.Add(BitConverter.ToUInt64(_data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.Short:
                            _packetObjects.Add(BitConverter.ToInt16(_data, bytepos));
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
            ReturnByteArray = null;
            byte[] NewData = new byte[_data.Length*2];
            _data.CopyTo(NewData, 0);
            _data = NewData;
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