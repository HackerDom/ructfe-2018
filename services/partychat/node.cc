#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <netinet/in.h>
#include <arpa/inet.h>

typedef unsigned short ushort;

bool parse_args(char *master_endpoint_str, const char *control_port_str, addrinfo **master_address, ushort *control_port) {
	
	char *ip_str = strtok(master_endpoint_str, ":");
	char *port_str = strtok(NULL, ":");

	if (ip_str == NULL || port_str == NULL)
		return false;

	if (getaddrinfo(ip_str, port_str, NULL, master_address) != 0)
		return false;

	*control_port = atoi(control_port_str);

	return true;
}

int main(int argc, char **argv) {

	addrinfo *master_address;
	ushort control_port;

	if (argc != 3 || !parse_args(argv[1], argv[2], &master_address, &control_port)) {
		printf("Usage:\n");
		printf("%s <master_endpoint> <control_port>\n", argv[0]);
		exit(-1);
	}

	printf("Running with args: %s:%d %d\n", 
		inet_ntoa(((sockaddr_in *)master_address->ai_addr)->sin_addr), 
		ntohs(((sockaddr_in *)master_address->ai_addr)->sin_port), 
		control_port);

	return 0;
}