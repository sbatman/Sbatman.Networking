// Example.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "../../Packet.h"
#include "../../BaseClient.h"
#include <synchapi.h>

using Sbatman::Networking::Packet;

int _tmain(int argc, _TCHAR* argv [])
{
	BaseClient * client = new BaseClient();
	client->Connect("sbatman.com", 6789);
	int64_t i = 0;
	while (true)
	{
		if (i>5000)break;
		Packet* p = new Packet(10);
		p->AddInt64(i);
		p->AddFloat(2.3f);
		client->SendPacket(p);

		vector<Packet*>  list = (client->GetPacketsToProcess());
		for (Packet * p : list)
		{
			printf("%d", p->GetType());
			delete p;
		}
		i++;
		Sleep(1);
	}

	client->Disconnect();
	delete client;

	return 0;
}

