#pragma once
#include "stdafx.h"
#include "Packet.h"

class BaseClient
{
public:
	BaseClient();
	~BaseClient();

	bool Connect(std::string serverAddress, uint32_t port, uint32_t recBufferSize = 50000);
	void SendPacket(Insanedev::Networking::Packet* packet);
	void SetForceNoDelay(bool state);
	bool GetFoceNoDelay() const;

private:

	SOCKET _InternalConnectSocket = INVALID_SOCKET;
	std::vector<Insanedev::Networking::Packet*> _PacketsToSend;
	std::thread * _PacketHandel;
	std::mutex _PacketListLock;
	std::mutex _SocketLock;

	bool SocketWrite(const uint8_t * data, uint32_t length);
	int SocketRead(uint8_t * data, uint32_t max);	
	void SocketClose();

	void Update();
	bool _Connected;

	uint32_t _RecBufferPosition;
	uint32_t _RecBufferSize;
	uint8_t * _RecBuffer;

};

