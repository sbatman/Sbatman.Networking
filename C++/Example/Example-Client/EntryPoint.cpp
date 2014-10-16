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
	client->Connect("127.0.0.1", 6789);
	int64_t i = 0;
	while (true){

		if ((i % 100) != 0)
		{
			Packet* p = new Packet(10);
			p->AddInt64(i);
			client->SendPacket(p);
		}
		else
		{
			vector<Packet*> * list = (client->GetPacketsToProcess());
			if (list != nullptr)
			{
				for (Packet * p : *list)
				{
					printf("%d", p->GetType());
					delete p;
				}
				delete list;
			}
		}
		i++;
		Sleep(1);
	}
	return 0;
}

