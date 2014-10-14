#include "stdafx.h"
#include "BaseClient.h"
#include "Packet.h"


BaseClient::BaseClient()
{
	_Connected = false;
}


BaseClient::~BaseClient()
{
	delete [] _RecBuffer;
}

bool BaseClient::Connect(std::string serverAddress, uint32_t port, uint32_t recBufferSize)
{
	{
		std::lock_guard<std::mutex> lock(_SocketLock);

		//setup recieve buffer
		_RecBufferSize = recBufferSize;
		_RecBuffer = new uint8_t[recBufferSize];
		_RecBufferPosition = 0;

		addrinfo *result = nullptr;
		addrinfo *ptr = nullptr;
		addrinfo hints;
		WSAData wsaData;

		ZeroMemory(&hints, sizeof(hints));
		hints.ai_family = AF_UNSPEC;
		hints.ai_socktype = SOCK_STREAM;
		hints.ai_protocol = IPPROTO_TCP;

		WSAStartup(MAKEWORD(2, 2), &wsaData);

		int32_t iResult = getaddrinfo(serverAddress.c_str(), std::to_string(port).c_str(), &hints, &result);
		if (iResult != 0)
		{
			printf("getaddrinfo failed: %d\n", iResult);
			WSACleanup();
			return 1;
		}

		ptr = result;

		_InternalConnectSocket = socket(ptr->ai_family, ptr->ai_socktype, ptr->ai_protocol);

		if (_InternalConnectSocket == INVALID_SOCKET) {

			printf("Error at socket(): %ld\n", WSAGetLastError());
			freeaddrinfo(result);
			WSACleanup();
			return 1;
		}

		iResult = connect(_InternalConnectSocket, ptr->ai_addr, static_cast<int32_t>(ptr->ai_addrlen));
		if (iResult == SOCKET_ERROR) {
			SocketClose();
		}

		SetForceNoDelay(true);

		_Connected = true;
		_PacketHandel = new std::thread(&BaseClient::Update, this);


		return true;
	}
}

void BaseClient::SendPacket(Insanedev::Networking::Packet* packet)
{
	{
		std::lock_guard<std::mutex> lock(_PacketListLock);
		_PacketsToSend.push_back(packet);
	}
}

bool BaseClient::GetFoceNoDelay() const
{
	bool flag;
	int len;
	getsockopt(_InternalConnectSocket, IPPROTO_TCP, TCP_NODELAY, reinterpret_cast<char *>(&flag), &len);
	return flag;
}

void BaseClient::SetForceNoDelay(bool state)
{
	bool flag = state;
	setsockopt(_InternalConnectSocket, IPPROTO_TCP, TCP_NODELAY, reinterpret_cast<char *>(&flag), sizeof(bool));
}

bool BaseClient::SocketWrite(const uint8_t * data, uint32_t length)
{
	int iResult = send(_InternalConnectSocket, reinterpret_cast<const char *>(data), length, 0);
	if (iResult == SOCKET_ERROR) {
		SocketClose();
		return false;
	}
	return true;
}

int BaseClient::SocketRead(uint8_t* data, uint32_t max)
{
	return recv(_InternalConnectSocket, reinterpret_cast<char*>(data), max, 0);
}

void BaseClient::SocketClose()
{
	closesocket(_InternalConnectSocket);
	_InternalConnectSocket = INVALID_SOCKET;
	WSACleanup();
}

void BaseClient::Update()
{
	while (_Connected)
	{
		//Sending packets

		std::vector<Insanedev::Networking::Packet*> _tempList;

		{
			std::lock_guard<std::mutex> lock(_PacketListLock);

			for (Insanedev::Networking::Packet* p : _PacketsToSend)
			{
				_tempList.push_back(p);
			}
			_PacketsToSend.clear();
		}

		if (_tempList.size() > 0)
		{
			{
				std::lock_guard<std::mutex> lock(_SocketLock);

				for (Insanedev::Networking::Packet* p : _tempList)
				{
					uint8_t * data;
					int length = p->ToByteArray(&data);

					SocketWrite(data, length);

					delete p;
				}

				_tempList.clear();
			}
		}

		_RecBufferPosition+=SocketRead(_RecBuffer + _RecBufferPosition, _RecBufferSize - _RecBufferPosition);

		// Packets

		bool finding = _RecBufferPosition > 11;
		while (finding)
		{
			bool packerstartpresent = true;
			for (int x = 0; x < 4; x++)
			{
				if (_RecBuffer[x] == Insanedev::Networking::Packet::PacketStartBytes[x]) continue;
				packerstartpresent = false;
				break;
			}
			if (packerstartpresent)
			{
				uint32_t size = *(_RecBuffer +6);
				if (_RecBufferPosition >= size)
				{
					Insanedev::Networking::Packet * p = Insanedev::Networking::Packet::FromByteArray(_RecBuffer);
					memcpy_s(_RecBuffer, _RecBufferSize, _RecBuffer + size, _RecBufferSize - size);
					_RecBufferPosition -= size;

					printf("%d",p->GetType());
					delete p;
				//	if (p != null) _PacketsToProcess.Enqueue(p);
				}
				else
				{
					finding = false;
				}
			}
			else
			{
				int offset = -1;
				for (int x = 0; x < _RecBufferPosition; x++)
				{
					if (_RecBuffer[x] == Insanedev::Networking::Packet::PacketStartBytes[x]) offset = x;
				}
				if (offset != -1)
				{
					memcpy_s(_RecBuffer, _RecBufferSize, _RecBuffer + offset, _RecBufferSize - offset);
					_RecBufferPosition -= offset;
				}
				else
				{
					_RecBufferPosition = 0;
				}
			}
			if (_RecBufferPosition < 12) finding = false;
		}
	}
}