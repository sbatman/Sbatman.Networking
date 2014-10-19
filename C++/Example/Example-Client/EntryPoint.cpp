// Example.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "../../Packet.h"
#include "../../BaseClient.h"
#include <synchapi.h>
#include <inttypes.h>

using Sbatman::Networking::Packet;

int _tmain(int argc, _TCHAR* argv [])
{
	int64_t i = 0;
	uint8_t *  byteArray = new uint8_t[20];
	for (int x = 0; x < 20; x++)
	{
		byteArray[x] = x;
	}


	BaseClient * client = new BaseClient();
	client->Connect("127.0.0.1", 6789);


	while (true)
	{
		//if (i > 5000)break;
		Packet* p = new Packet(10);
		p->AddInt64(i);
		p->AddFloat(2.3f);
		p->AddByteArray(byteArray,20);
		client->SendPacket(p);

		vector<Packet*>  list = (client->GetPacketsToProcess());
		for (Packet * p : list)
		{
			//printf("%d", p->GetType());
			std::vector<Packet::PakObject> packedObjects = p->GetObjects();
			uint8_t * data = static_cast<uint8_t *>(packedObjects[0].Ptr);

			printf("%" PRIu8 ",%" PRIu8 ",%" PRIu8 ",%" PRIu8 ",%" PRIu8 "\n", data[0], data[1], data[2], data[3], data[4]);
			delete p;
		}
		i++;
		Sleep(1);
	}

	client->Disconnect();
	delete client;

	return 0;
}

