// Example.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "../../Packet.h"
#include "../../BaseClient.h"

using Insanedev::Networking::Packet;

int _tmain(int argc, _TCHAR* argv [])
{
	BaseClient * client = new BaseClient();
	client->Connect("127.0.0.1", 6789);

	return 0;
}

