// Example.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "../../Packet.h"



int _tmain(int argc, _TCHAR* argv [])
{
	for (int a = 0; a < 100000; a++){
		Packet * p = new Packet(static_cast<uint16_t>(14));
		for (int x = 0; x < 1000; x++)
		{
			p->AddFloat(14.6f);
			p->AddDouble(14.6);
			p->AddInt32(14);
		}
		uint8_t* data;
		int datasize = p->ToByteArray(&data);

		delete p;
	}

	return 0;
}

