#pragma once
#include <stdint.h>
#include <stdbool.h>
#include <float.h>
#include <vector>

namespace Sbatman
{
	namespace Networking
	{
		/// <summary>
		///     The Packet class is a light class that is used for serialising and deserialising data into and from the network
		///     stream. This allows easy passing over variables over the network and guarantees order.
		/// </summary>
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

			typedef std::tuple<void *, Packet::ParamTypes> PakObject;

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
			///     adds the unsigned int16 provided to the packet
			/// </summary>
			void AddUint16(const uint16_t value);
			/// <summary>
			///     Adds the int32 provided to the packet
			/// </summary>
			void AddInt32(const int32_t value);
			/// <summary>
			///     Adds the unsigned int32 provided to the packet
			/// </summary>
			void AddUint32(const uint32_t value);
			/// <summary>
			///     Adds the int64 provided to the packet
			/// </summary>
			void  AddInt64(const int64_t value);
			/// <summary>
			///     Adds the unsigned int64 provided to the packet
			/// </summary>
			void AddUint64(const uint64_t value);
			/// <summary>
			///     Adds the boolean provided to the packet
			/// </summary>
			void AddBool(const bool value);

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
			std::vector<PakObject> GetObjects() const;
			/// <summary>
			///     Returns the type id of this packet
			/// </summary>
			uint16_t GetType() const;

			/// <summary>
			///    Creates a packet from tje provided byte aray and returns 
			///    a pointer to the packet
			/// </summary>
			static Packet * FromByteArray(const uint8_t * data);

			/// <summary>
			///    These bytes are added tot he front of ever packet to help identify 
			///    a packet start in a byte stream
			/// </summary>
			static const uint8_t PacketStartBytes[4];


		protected:
			/// <summary>
			///     A pointer to the internal data array of the packet
			/// </summary>
			uint8_t * _Data = nullptr;
			/// <summary>
			///     The current size of the internal data array of the packet
			/// </summary>
			uint32_t _DataArraySize = 0;
			/// <summary>
			///     The first positing in the data array that has yet to be written to
			/// </summary>
			uint32_t _DataPos;
			/// <summary>
			///     A vector to pointers of the packet objects
			/// </summary>
			std::vector<PakObject> _PackedObjects;
			/// <summary>
			///     The number of objects that are stored in the packet
			/// </summary>
			uint16_t _ParamCount;
			/// <summary>
			///     A pointer to the built array ready used when returning the packet as a byte array
			/// </summary>
			uint8_t * _ReturnByteArray = nullptr;
			/// <summary>
			///     The current size of the return byte array
			/// </summary>
			uint32_t _ReturnByteArraySize = 0;

		private:
			Packet();
			/// <summary>
			///     The type id of this packet, used in packet processing
			/// </summary>
			uint16_t _Type;
			/// <summary>
			///     Destroys the internal return byte array
			/// </summary>
			void DestroyReturnByteArray();
			/// <summary>
			///     Doubles the size of the internal data array, realocating it and copying
			///		the exsisting data into the new array
			/// </summary>
			void ExpandDataArray();

			/// <summary>
			///     Returns the data from the specific address typecast to the specified type
			/// </summary>
			template <typename T>
			T * GetDataFromArray(int offset);

			/// <summary>
			///     Adds the provided data to the specified position in the data array
			/// </summary>
			template <typename T>
			void AddToDataArray(ParamTypes type, int32_t dataAmmount, T const*  dataPosition);

			/// <summary>
			///     Rebuilds the vector of stored values from the data array
			/// </summary>
			void UpdateObjects();


			/// <summary>
			///     Deletes the internal vector of packed objects in a memory safe way
			/// </summary>
			void DeletePackedObjects();
		};
	}
}