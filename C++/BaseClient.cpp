#include "stdafx.h"
#include "BaseClient.h"
#include "WS2tcpip.h"
#include <string>

BaseClient::BaseClient()
{
}


BaseClient::~BaseClient()
{
}

bool BaseClient::Connect(std::string serverAddress, uint32_t port, int bufferSize)
{
	struct addrinfo *result = nullptr, *ptr = nullptr, hints;

	ZeroMemory(&hints, sizeof(hints));
	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	WSAData wsaData;

	int err = WSAStartup(MAKEWORD(2, 2), &wsaData);

	int iResult = getaddrinfo(serverAddress.c_str(), std::to_string(port).c_str(), &hints, &result);
	if (iResult != 0)
	{
		printf("getaddrinfo failed: %d\n", iResult);
		WSACleanup();
		return 1;
	}
	SOCKET ConnectSocket = INVALID_SOCKET;
	ptr = result;

	ConnectSocket = socket(ptr->ai_family, ptr->ai_socktype,
		ptr->ai_protocol);
	if (ConnectSocket == INVALID_SOCKET) {
		printf("Error at socket(): %ld\n", WSAGetLastError());
		freeaddrinfo(result);
		WSACleanup();
		return 1;
	}

	iResult = connect(ConnectSocket, ptr->ai_addr, static_cast<int>(ptr->ai_addrlen));
	if (iResult == SOCKET_ERROR) {
		closesocket(ConnectSocket);
		ConnectSocket = INVALID_SOCKET;
	}

	char *sendbuf = "this is a test";
	iResult = send(ConnectSocket, sendbuf, static_cast<int>(strlen(sendbuf)), 0);
	if (iResult == SOCKET_ERROR) {
		printf("send failed: %d\n", WSAGetLastError());
		closesocket(ConnectSocket);
		WSACleanup();
		return 1;
	}

	return true;
}