// Example.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "../../Packet.h"



int _tmain(int argc, _TCHAR* argv [])
{
	for (int a = 0; a < 100000; a++){
		Packet * p = new Packet(static_cast<uint16_t>(14));
		for (int x = 0; x < 10; x++)
		{
			p->AddFloat(14.6f);
			p->AddDouble(4.5556);
		}
		uint8_t* data;
		int datasize = p->ToByteArray(&data);
		Packet * d = Packet::FromByteArray(data);
		delete p;
		float_t f = *static_cast<float_t *>(d->GetObjects()[0]);
		double_t theDouble = *static_cast<double_t *>(d->GetObjects()[1]);

		delete d;
	}

	return 0;
}

