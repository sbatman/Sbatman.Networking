#include "stdafx.h"
#include "Packet.h"

using namespace Sbatman::Networking;

const uint8_t Packet::PacketStartBytes[4] = { 0, 48, 21, 0 };
static const uint32_t INITIAL_DATA_SIZE = 128;
static const uint32_t PACKET_HEADER_LENGTH = 12;

Packet::Packet(uint16_t type)
{
	_Data = new uint8_t[INITIAL_DATA_SIZE];
	_DataArraySize = INITIAL_DATA_SIZE;
	_DataPos = 0;
	_PackedObjects = std::vector<PakObject>();
	_ParamCount = 0;
	_Type = type;
	_ReturnByteArray = nullptr;
}

void Packet::AddFloat(float_t const value)
{
	AddToDataArray<float_t>(FLOAT, sizeof(float_t), &value);
}

void Packet::AddDouble(double_t const value)
{
	AddToDataArray<double_t>(DOUBLE, sizeof(double_t), &value);
}

void Packet::AddInt16(int16_t const value)
{
	AddToDataArray<int16_t>(INT16, sizeof(int16_t), &value);
}

void Packet::AddUint16(uint16_t const value)
{
	AddToDataArray<uint16_t>(UINT16, sizeof(uint16_t), &value);
}

void Packet::AddInt32(int32_t const value)
{
	AddToDataArray<int32_t>(INT32, sizeof(int32_t), &value);
}

void Packet::AddUint32(uint32_t const value)
{
	AddToDataArray<uint32_t>(UINT32, sizeof(uint32_t), &value);
}

void Packet::AddInt64(int64_t const value)
{
	AddToDataArray<int64_t>(INT64, sizeof(int64_t), &value);
}

void Packet::AddUint64(uint64_t const value)
{
	AddToDataArray<uint64_t>(UINT64, sizeof(uint64_t), &value);
}

void Packet::AddBool(bool const value)
{
	AddToDataArray<bool>(BOOL, sizeof(uint64_t), &value);
}

void Packet::AddByteArray(uint8_t const* value, uint32_t const length)
{
	DestroyReturnByteArray();
	while (_DataPos + (length + 5) >= _DataArraySize) ExpandDataArray();
	_Data[_DataPos++] = static_cast<uint8_t>(BYTE_PACKET);
	memcpy_s(_Data + _DataPos, _DataArraySize, &length, sizeof(uint32_t));
	_DataPos += 4;
	memcpy_s(_Data + _DataPos, _DataArraySize, value, length);
	_DataPos += length;
	_ParamCount++;
}

const uint32_t Packet::ToByteArray(uint8_t ** dataPointer)
{
	if (_ReturnByteArray == nullptr)
	{
		_ReturnByteArraySize = _DataPos + PACKET_HEADER_LENGTH;
		_ReturnByteArray = new uint8_t[_ReturnByteArraySize];
		memcpy_s(_ReturnByteArray, _ReturnByteArraySize, PacketStartBytes, sizeof(PacketStartBytes));
		memcpy_s(_ReturnByteArray + 4, _ReturnByteArraySize, &_ParamCount, sizeof(_ParamCount));
		memcpy_s(_ReturnByteArray + 6, _ReturnByteArraySize, &_ReturnByteArraySize, sizeof(_ReturnByteArraySize));
		memcpy_s(_ReturnByteArray + 10, _ReturnByteArraySize, &_Type, sizeof(_Type));
		memcpy_s(_ReturnByteArray + 12, _ReturnByteArraySize, _Data, _DataPos);
	}
	(*dataPointer) = _ReturnByteArray;
	return _ReturnByteArraySize;
}

std::vector<Packet::PakObject> Packet::GetObjects() const
{
	return _PackedObjects;
}

uint16_t Packet::GetType() const
{
	return _Type;
}

Packet * Packet::FromByteArray(const uint8_t* data)
{
	Packet * p = new Packet();
	memcpy_s(&p->_ParamCount, sizeof(p->_ParamCount), data + 4, sizeof(uint16_t));
	memcpy_s(&p->_DataArraySize, sizeof(p->_DataArraySize), data + 6, sizeof(uint32_t));
	memcpy_s(&p->_Type, sizeof(p->_Type), data + 10, sizeof(uint16_t));
	p->_DataArraySize -= PACKET_HEADER_LENGTH;
	p->_Data = new uint8_t[p->_DataArraySize];
	memcpy_s(p->_Data, p->_DataArraySize, (data + 12), p->_DataArraySize);
	p->_DataPos = p->_DataArraySize;
	p->UpdateObjects();
	return p;
}

Packet::Packet()
{
}

void Packet::DestroyReturnByteArray()
{
	if (_ReturnByteArray != nullptr)
	{
		delete [] _ReturnByteArray;
		_ReturnByteArray = nullptr;
		_ReturnByteArraySize = 0;
	}
}

void Packet::ExpandDataArray()
{
	uint8_t * oldData = _Data;
	uint32_t oldDataSize = _DataArraySize;
	_Data = new uint8_t[_DataArraySize * 2];
	_DataArraySize = _DataArraySize * 2;
	memcpy_s(_Data, sizeof(_Data) *_DataArraySize, oldData, oldDataSize);
	delete [] oldData;
}

void Packet::UpdateObjects()
{
	DeletePackedObjects();

	int ArrayPos = 0;
	for (uint16_t i = 0; i < _ParamCount; ++i)
	{
		uint8_t type = 0;
		memcpy_s(&type, sizeof(uint8_t), _Data + ArrayPos, sizeof(uint8_t));
		ArrayPos += 1;
		switch (static_cast<ParamTypes>(type))
		{
			case FLOAT:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<float_t>(ArrayPos)), FLOAT, sizeof(float_t)));
				ArrayPos += sizeof(float_t);
				break;
			case DOUBLE:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<double_t>(ArrayPos)), DOUBLE, sizeof(double_t)));
				ArrayPos += sizeof(double_t);
				break;
			case INT16:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<int16_t>(ArrayPos)), INT16, sizeof(int16_t)));
				ArrayPos += sizeof(int16_t);
				break;
			case UINT16:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<uint16_t>(ArrayPos)), UINT16, sizeof(uint16_t)));
				ArrayPos += sizeof(uint16_t);
				break;
			case INT32:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<int32_t>(ArrayPos)), INT32, sizeof(int32_t)));
				ArrayPos += sizeof(int32_t);
				break;
			case UINT32:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<uint32_t>(ArrayPos)), UINT32, sizeof(uint32_t)));
				ArrayPos += sizeof(uint32_t);
				break;
			case INT64:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<int64_t>(ArrayPos)), INT64, sizeof(int64_t)));
				ArrayPos += sizeof(int64_t);
				break;
			case UINT64:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<uint64_t>(ArrayPos)), UINT64, sizeof(uint64_t)));
				ArrayPos += sizeof(uint64_t);
				break;
			case BOOL:
				_PackedObjects.push_back(PakObject(static_cast<void *>(GetDataFromArray<bool>(ArrayPos)), BOOL, sizeof(bool)));
				ArrayPos += sizeof(bool);
				break;
			case BYTE_PACKET:
			{
				uint8_t ** dataPtr = new uint8_t *;
				int length = GetDataFromArray(ArrayPos, dataPtr);
				_PackedObjects.push_back(PakObject(static_cast<void *>(*dataPtr), BYTE_PACKET, length));
				ArrayPos += (length + 4);
				delete dataPtr;
			}
				break;
			case UTF8_STRING: break;
			case COMPRESSED_BYTE_PACKET: break;
			default: break;
		}
	}
}

void Packet::DeletePackedObjects()
{
	for (PakObject object : _PackedObjects)
	{
		switch (object.Type)
		{
			case FLOAT:		delete static_cast<float *>(object.Ptr); break;
			case DOUBLE:	delete static_cast<double *>(object.Ptr); break;
			case INT16:		delete static_cast<int16_t *>(object.Ptr); break;
			case UINT16:	delete static_cast<uint16_t *>(object.Ptr); break;
			case INT32:		delete static_cast<int32_t *>(object.Ptr); break;
			case UINT32:	delete static_cast<uint32_t *>(object.Ptr); break;
			case INT64:		delete static_cast<int64_t *>(object.Ptr); break;
			case UINT64:	delete static_cast<uint64_t *>(object.Ptr); break;
			case BOOL:		delete static_cast<bool *>(object.Ptr); break;
			case BYTE_PACKET:
				delete [] static_cast<uint8_t *>(object.Ptr);
				break;
			case UTF8_STRING: break;
			case COMPRESSED_BYTE_PACKET: break;
			default: break;
		}

	}
	_PackedObjects.clear();
}

template <typename T>
T * Packet::GetDataFromArray(int offset)
{
	T * value = new T(0);
	memcpy_s(value, sizeof(T), _Data + offset, sizeof(T));
	return value;
}

uint32_t  Packet::GetDataFromArray(int offset, uint8_t ** pointer)
{
	uint32_t arrayLength = 0;
	memcpy_s(&arrayLength, sizeof(uint32_t), _Data + offset, sizeof(uint32_t));
	(*pointer) = new uint8_t[arrayLength];
	offset += sizeof(uint32_t);
	memcpy_s((*pointer), arrayLength, _Data + offset, arrayLength);
	return arrayLength;
}

template <typename T>
void Packet::AddToDataArray(ParamTypes type, int32_t dataAmmount, T const *  dataPosition)
{
	DestroyReturnByteArray();
	while (_DataPos + (dataAmmount + 1) >= _DataArraySize) ExpandDataArray();
	_Data[_DataPos++] = static_cast<uint8_t>(type);
	memcpy_s(_Data + _DataPos, _DataArraySize, dataPosition, dataAmmount);
	_DataPos += dataAmmount;
	_ParamCount++;
}

Packet::~Packet()
{
	if (_ReturnByteArray != nullptr)
	{
		delete [] _ReturnByteArray;
		_ReturnByteArray = nullptr;
		_ReturnByteArraySize = 0;
	}
	delete [] _Data;
	_DataPos = 0;
	DeletePackedObjects();

	_ParamCount = 0;
	_Type = 0;
}

