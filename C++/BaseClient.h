#pragma once
#include "stdafx.h"
#include "Packet.h"

using Insanedev::Networking::Packet;
using namespace std;

class BaseClient
{
public:
	BaseClient();
	~BaseClient();

	bool Connect(string serverAddress, uint32_t port, uint32_t recBufferSize = 50000);
	void SendPacket(Packet* packet);
	vector<Packet*> * GetPacketsToProcess();
	uint32_t GetPacketsToProcessCount();
	uint32_t GetPacketsToSendCount();
	bool IsConnected() const;

	void SetForceNoDelay(bool state);
	bool GetFoceNoDelay() const;

	void Disconnect();

private:

	SOCKET _InternalConnectSocket = INVALID_SOCKET;
	vector<Packet*> _PacketsToSend;
	vector<Packet*> _PacketsToProcess;

	thread * _PacketHandel;
	mutex _PacketListLock;
	mutex _ProcessPacketListLock;
	mutex _SocketLock;

	bool SocketWrite(const uint8_t * data, uint32_t length);
	int SocketRead(uint8_t * data, uint32_t max);	
	void SocketClose();

	void Update();
	bool _Connected;

	uint32_t _RecBufferPosition;
	uint32_t _RecBufferSize;
	uint8_t * _RecBuffer;

};

