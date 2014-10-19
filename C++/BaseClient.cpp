#include "stdafx.h"
#include "BaseClient.h"
#include "Packet.h"

using namespace Sbatman::Networking;

BaseClient::BaseClient()
{
	_Connected = false;
	_RecBuffer = nullptr;
}


BaseClient::~BaseClient()
{
	if (_RecBuffer != nullptr)
	{
		delete [] _RecBuffer;
		_RecBuffer = nullptr;
	}
	if (_InternalConnectSocket != INVALID_SOCKET) SocketClose();
	_Connected = false;
	_PacketHandel->join();
	delete _PacketHandel;
	for (Packet * packet : _PacketsToSend) delete packet;
	for (Packet * packet : _PacketsToProcess) delete packet;
}

bool BaseClient::Connect(string serverAddress, uint32_t port, uint32_t recBufferSize)
{
	{
		lock_guard<mutex> lock(_SocketLock);

		_ASSERT(_InternalConnectSocket == INVALID_SOCKET);
		_ASSERT(!_Connected);

		//
		if (_RecBuffer != nullptr)
		{
			delete [] _RecBuffer;
			_RecBuffer = nullptr;
		}
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

		int32_t iResult = getaddrinfo(serverAddress.c_str(), to_string(port).c_str(), &hints, &result);
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
		_PacketHandel = new thread(&BaseClient::Update, this);



		return true;
	}
}

void BaseClient::SendPacket(Packet* packet)
{
	if (_InternalConnectSocket != INVALID_SOCKET && _Connected)
	{
		lock_guard<mutex> lock(_PacketListLock);
		_PacketsToSend.push_back(packet);
	}
	else
	{
		throw NotConnectedException();
	}
}

vector<Packet*> BaseClient::GetPacketsToProcess()
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		vector<Packet*> returnList = vector<Packet*>();

		{
			lock_guard<mutex> lock(_ProcessPacketListLock);

			if (_PacketsToProcess.size() == 0) return returnList;

			for (Packet * p : _PacketsToProcess) returnList.push_back(p);

			_PacketsToProcess.clear();

		}
		return returnList;
	}
	else
	{
		throw NotConnectedException();
	}
}

uint32_t BaseClient::GetPacketsToProcessCount()
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		lock_guard<mutex> lock(_ProcessPacketListLock);
		return static_cast<uint32_t>(_PacketsToProcess.size());
	}
	throw NotConnectedException();
}

uint32_t BaseClient::GetPacketsToSendCount()
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		lock_guard<mutex> lock(_PacketListLock);
		return static_cast<uint32_t>(_PacketsToSend.size());
	}
	throw NotConnectedException();
}

bool BaseClient::IsConnected() const
{
	return _Connected;
}

bool BaseClient::GetFoceNoDelay() const
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		bool flag;
		int len;
		getsockopt(_InternalConnectSocket, IPPROTO_TCP, TCP_NODELAY, reinterpret_cast<char *>(&flag), &len);
		return flag;
	}
	throw NotConnectedException();
}

void BaseClient::Disconnect()
{
	{
		lock_guard<mutex> lock(_SocketLock);
		SocketClose();
	}
}

void BaseClient::SetForceNoDelay(bool state)
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		bool flag = state;
		setsockopt(_InternalConnectSocket, IPPROTO_TCP, TCP_NODELAY, reinterpret_cast<char *>(&flag), sizeof(bool));
	}
	else
	{
		throw NotConnectedException();
	}
}

bool BaseClient::SocketWrite(const uint8_t * data, uint32_t length)
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		int iResult = send(_InternalConnectSocket, reinterpret_cast<const char *>(data), length, 0);
		if (iResult == SOCKET_ERROR) {
			SocketClose();
			return false;
		}
		return true;
	}
	throw NotConnectedException();
}

int BaseClient::SocketRead(uint8_t* data, uint32_t max)
{
	if (_InternalConnectSocket != INVALID_SOCKET  && _Connected)
	{
		try
		{
			u_long iMode = 1;
			ioctlsocket(_InternalConnectSocket, FIONBIO, &iMode);
			int bytes = recv(_InternalConnectSocket, reinterpret_cast<char*>(data), max, 0);
			iMode = 0;
			ioctlsocket(_InternalConnectSocket, FIONBIO, &iMode);
			return bytes < 0 ? 0 : bytes;
		}
		catch (exception e)
		{
			SocketClose();
			return 0;
		}
	}
	throw NotConnectedException();
}

void BaseClient::SocketClose()
{
	closesocket(_InternalConnectSocket);
	_InternalConnectSocket = INVALID_SOCKET;
	WSACleanup();
	if (_RecBuffer != nullptr)
	{
		delete [] _RecBuffer;
		_RecBuffer = nullptr;
	}
}

void BaseClient::Update()
{
	while (_Connected)
	{
		if (_InternalConnectSocket == INVALID_SOCKET)
		{
			_Connected = false;
			return;
		}
		//Sending packets

		vector<Packet*> _tempList;

		{
			lock_guard<mutex> lock(_PacketListLock);

			for (Packet* p : _PacketsToSend)
			{
				_tempList.push_back(p);
			}
			_PacketsToSend.clear();
		}

		if (_tempList.size() > 0)
		{
			{
				lock_guard<mutex> lock(_SocketLock);

				for (Packet* p : _tempList)
				{
					uint8_t * data;
					int length = p->ToByteArray(&data);

					SocketWrite(data, length);

					delete p;
				}

				_tempList.clear();
			}
		}

		_RecBufferPosition += SocketRead(_RecBuffer + _RecBufferPosition, _RecBufferSize - _RecBufferPosition);

		// Recievning Packets

		bool finding = _RecBufferPosition > 11;
		while (finding)
		{
			bool packerstartpresent = true;
			for (int x = 0; x < 4; x++)
			{
				if (_RecBuffer[x] == Packet::PacketStartBytes[x]) continue;
				packerstartpresent = false;
				break;
			}
			if (packerstartpresent)
			{
				uint32_t size = *(_RecBuffer + 6);
				if (_RecBufferPosition >= size)
				{
					Packet * p = Packet::FromByteArray(_RecBuffer);
					memcpy_s(_RecBuffer, _RecBufferSize, _RecBuffer + size, _RecBufferSize - size);
					_RecBufferPosition -= size;

					{
						lock_guard<mutex> lock(_ProcessPacketListLock);
						_PacketsToProcess.push_back(p);
					}
				}
				else
				{
					finding = false;
				}
			}
			else
			{
				int offset = -1;
				for (uint32_t x = 0; x < _RecBufferPosition; x++)
				{
					if (_RecBuffer[x] == Packet::PacketStartBytes[x]) offset = x;
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