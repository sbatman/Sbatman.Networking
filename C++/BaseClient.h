#pragma once
class BaseClient
{
public:
	BaseClient();
	~BaseClient();

	bool Connect(std::string serverAddress, uint32_t port, int bufferSize = 50000);
};

