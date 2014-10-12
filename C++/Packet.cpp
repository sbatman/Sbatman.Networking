#include "stdafx.h"
#include "Packet.h"

static const uint8_t PacketStartBytes[4] = { 0, 48, 21, 0 };
static const uint32_t INITIAL_DATA_SIZE = 128;

Packet::Packet(uint16_t type)
{
	_Data = new uint8_t[INITIAL_DATA_SIZE];
	_DataArraySize = INITIAL_DATA_SIZE;
	_DataPos = 0;
	_PackedObjects = std::vector<void *>();
	_ParamCount = 0;
	_Type = type;
}

void Packet::AddFloat(float_t const value)
{
	AddToDataArray<float_t>(FLOAT, sizeof(float_t), &value);
}

void Packet::AddDouble(double_t const value)
{
	AddToDataArray<double_t>(DOUBLE, sizeof(double_t),  &value);
}

void Packet::AddInt16(int16_t const value)
{
	AddToDataArray<int16_t>(INT16, sizeof(int16_t),&value);
}

void Packet::AddUint16(uint16_t const value)
{
	AddToDataArray<uint16_t>(UINT16, sizeof(uint16_t),  &value);
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

void Packet::DestroyReturnByteArray()
{
	if (_ReturnByteArray != nullptr)
	{
		delete [] _ReturnByteArray;
		_ReturnByteArray = nullptr;
	}
}

void Packet::ExpandDataArray()
{
	uint8_t * oldData = _Data;
	uint32_t oldDataSize = _DataArraySize;
	_Data = new uint8_t[_DataArraySize * 2];
	_DataArraySize = _DataArraySize * 2;
	memcpy_s(_Data, sizeof(_Data)*_DataArraySize, oldData, oldDataSize);
	delete [] oldData;
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
	delete [] _Data;
	_DataPos = 0;
	for (void * ptr : _PackedObjects)delete ptr;
	_ParamCount = 0;
	_Type = 0;
}
