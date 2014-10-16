#pragma once
#include "stdafx.h"
#include "Packet.h"

using Insanedev::Networking::Packet;
using namespace std;

/// <summary>
/// A simple client that can connect to insanedev.networking servers and other servers
/// using the same packet layout
/// </summary>
class BaseClient
{
public:
	BaseClient();
	~BaseClient();

	/// <summary>
	/// Connects to the server at the specified address with the given buffer size
	/// </summary>
	bool Connect(string serverAddress, uint32_t port, uint32_t recBufferSize = 50000);
	/// <summary>
	/// Sends a packet which will be deleted once sent
	/// </summary>
	void SendPacket(Packet* packet);
	/// <summary>
	/// Returns a vector of pointers to packets that have to be processed, you assume resposibility
	/// for deleting the packets once you have processed them.
	/// </summary>
	vector<Packet*> * GetPacketsToProcess();
	/// <summary>
	/// Returns the current number of packets that have arrived and are awaiting processing
	/// </summary>
	uint32_t GetPacketsToProcessCount();
	/// <summary>
	/// Returns the current number of packets that have yet to be sent
	/// </summary>
	uint32_t GetPacketsToSendCount();
	/// <summary>
	/// Returns whether this socket is currently connected or not
	/// </summary>
	bool IsConnected() const;

	/// <summary>
	/// Sets a flag to forcefully disable the Nagel algorithm, more info
	/// here : http://support2.microsoft.com/kb/214397
	/// </summary>
	void SetForceNoDelay(bool state);
	/// <summary>
	/// Retrieves whether the Nagel algorithm has been forcefully disabled, more info
	/// here : http://support2.microsoft.com/kb/214397
	/// </summary>
	bool GetFoceNoDelay() const;

	/// <summary>
	/// Disconnects the socket
	/// </summary>
	void Disconnect();

private:

	/// <summary>
	/// Internal socket used for interacting with the winsockets api
	/// </summary>
	SOCKET _InternalConnectSocket = INVALID_SOCKET;
	/// <summary>
	/// A vector of pointers to the packets that have yet to be sent
	/// </summary>
	vector<Packet*> _PacketsToSend;
	/// <summary>
	/// S vector of pointers to the packets that have arrived but have not yet been taken by the user
	/// </summary>
	vector<Packet*> _PacketsToProcess;

	/// <summary>
	/// A pointer to the internal thread that handels recieving and sending packets
	/// </summary>
	thread * _PacketHandel;
	/// <summary>
	/// A mutex protecting the outgoing packet list
	/// </summary>
	mutex _PacketListLock;
	/// <summary>
	/// A mutex protecting the incoming packet list
	/// </summary>
	mutex _ProcessPacketListLock;
	/// <summary>
	/// A mutex for proecting socket specific actions
	/// </summary>
	mutex _SocketLock;

	/// <summary>
	/// The position in the recieving buffer that data has been populated
	/// </summary>
	uint32_t _RecBufferPosition;
	/// <summary>
	/// The size of the recieving buffer
	/// </summary>
	uint32_t _RecBufferSize;
	/// <summary>
	/// S pointer to the recieving buffer
	/// </summary>
	uint8_t * _RecBuffer;

	/// <summary>
	/// Writes length bytes starting a tthe pointer data to the underlying socket
	/// </summary>
	bool SocketWrite(const uint8_t * data, uint32_t length);
	/// <summary>
	/// Reads as many as avaliable bytes from the underlying socket into the array
	/// address starting at data with a limit of max bytes
	/// </summary>
	int SocketRead(uint8_t * data, uint32_t max);
	/// <summary>
	/// Closes the underlying socket
	/// </summary>
	void SocketClose();

	/// <summary>
	/// The thread function for the thread _PacketHandel.
	/// </summary>
	void Update();
	/// <summary>
	/// A boolean represnting whether the socket is considered connected
	/// </summary>
	bool _Connected;
};

