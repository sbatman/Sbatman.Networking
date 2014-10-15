#pragma once
#include <stdint.h>
#include <stdbool.h>
#include <float.h>
#include <vector>
namespace Insanedev
{
	namespace Networking
	{

		class Packet
		{
		public:

     /// <summary>
     ///     The types that can be added to a packet
     /// </summary>
			enum ParamTypes
			{
				FLOAT,
				DOUBLE,
				INT16,
				UINT16,
				INT32,
				UINT32,
				INT64,
				UINT64,
				BOOL,
				BYTE_PACKET,
				UTF8_STRING,
				COMPRESSED_BYTE_PACKET,
			};

			Packet(uint16_t type);
			~Packet();
			
     /// <summary>
     ///     Adds the float provided to the packet
     /// </summary>
			void AddFloat(const float_t value);
     /// <summary>
     ///     Adds the double provided to the packet
     /// </summary>
			void AddDouble(const double_t value);
			/// <summary>
     ///     Adds the int16 provided to the packet
     /// </summary>
			void AddInt16(const int16_t value);
     /// <summary>
     ///     adds the unsigned int16 ptovided to the packet
     /// </summary>
			void AddUint16(const uint16_t value);
     /// <summary>
     ///     Adds the int32 ptovided to the packet
     /// </summary>
			void AddInt32(const int32_t value);
     /// <summary>
     ///     Adds the unsigned int32 ptovided to the packet
     /// </summary>
			void AddUint32(const uint32_t value);
     /// <summary>
     ///     Adds the int64 ptovided to the packet
     /// </summary>
			void  AddInt64(const int64_t value);
     /// <summary>
     ///     adds the unsigned int64 ptovided to the packet
     /// </summary>
			void AddUint64(const uint64_t value);
			
			/// <summary>
     ///     Generates a byte arry from the packet returning the length in bytes
     ///     and populating the pointer provided. The pointer will become invalid
     ///     id the packet is deleted.
     /// </summary>
			const uint32_t ToByteArray(uint8_t ** dataPointer);
     /// <summary>
     ///     Retutns a vector containing pointers to all the objects in this packet
     ///     The pointers will become invalid if this packet is deleted
     /// </summary>
			std::vector<void *> GetObjects() const;
			/// <summary>
     ///     Returns the type id of this packet
     /// </summary>
			uint16_t GetType() const;
			
     /// <summary>
     ///    Creates a packet from tje provided byte aray and returns 
     ///    a pointer to the packet
     /// </summary>
     static Packet * FromByteArray(const uint8_t * data);
			static const uint8_t PacketStartBytes[4];


		protected:
			uint8_t * _Data = nullptr;
			uint32_t _DataArraySize = 0;
			uint32_t _DataPos;
			std::vector<void *> _PackedObjects;
			uint16_t _ParamCount;
			uint8_t * _ReturnByteArray = nullptr;
			uint32_t _ReturnByteArraySize = 0;

		private:
			Packet();
			uint16_t _Type;
			void DestroyReturnByteArray();
			void ExpandDataArray();
			template <typename T>
			T * GetDataFromArray(int offset);
			template <typename T>
			void AddToDataArray(ParamTypes type, int32_t dataAmmount, T const*  dataPosition);
			void UpdateObjects();
		};

	}
}