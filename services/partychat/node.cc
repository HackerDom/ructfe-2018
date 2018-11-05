#include <time.h>
#include <poll.h>

#include "common.h"
#include "commands.h"

typedef unsigned short ushort;

bool parse_args(char *master_endpoint_str, const char *control_port_str, addrinfo **master_address, ushort *control_port) {
	
	if (!pc_parse_endpoint(master_endpoint_str, master_address))
		return false;

	*control_port = atoi(control_port_str);

	return true;
}

int find_empty_slot(pollfd *fds, int length) {
	for (int i = 0; i < length; i++) {
		if (!fds[i].fd)
			return i;
	}
	return -1;
}

#define FDS_CT 8

int main(int argc, char **argv) {

	addrinfo *master_address;
	ushort control_port;

	if (argc != 3 || !parse_args(argv[1], argv[2], &master_address, &control_port)) {
		printf("Usage:\n");
		printf("%s <master_endpoint> <control_port>\n", argv[0]);
		exit(-1);
	}

	char log_file[64];
	sprintf(log_file, "%s.log", argv[0]);
	pc_init_logging(log_file, true);

	pc_log("Running with args: %s:%d %d", 
		inet_ntoa(((sockaddr_in *)master_address->ai_addr)->sin_addr), 
		ntohs(((sockaddr_in *)master_address->ai_addr)->sin_port), 
		control_port);

	master_link uplink(*master_address);

	int server_sock = pc_start_server(control_port);

	pollfd fds[FDS_CT + 1];
	memset(&fds, 0, sizeof(fds));
	int used_fds = FDS_CT + 1;

	fds[0].fd = server_sock;
	fds[0].events = POLLIN;

	controller *controllers[FDS_CT];

	while (true) {

		uplink.tick();
		pc_log("Master: %s", uplink.hb.master_available ? "available" : "unavailable");

		int result = poll(fds, used_fds, 1000);
		if (result < 0)
			pc_fatal("main: poll() failed unexpectedly.");

		for (int i = 0; i < used_fds; i++) {
			if (fds[i].revents != POLLIN)
				continue;

			if (i == 0) {
				int client_idx = find_empty_slot(fds + 1, FDS_CT);

				if (client_idx >= 0) {
					int client_sock = pc_accept_client(server_sock);
					if (client_sock) {
						controllers[client_idx] = new controller(client_sock);
						fds[client_idx + 1].fd = client_sock;
						fds[client_idx + 1].events = POLLIN;
					}
					pc_log("main: accepted a new controlling connection.");
				}
				else {
					pc_log("main: cannot accept more controlling connections.");
					fds[0].events = 0;
				}
				continue;
			}

			if (!controllers[i - 1]->tick()) {
				delete controllers[i - 1];
				controllers[i - 1] = NULL;
				bzero(&fds[i], sizeof(pollfd));
				fds[0].events = POLLIN;
			}
		}
	}

	for (int i = 0; i < FDS_CT; i++) {
		delete controllers[i];
	} 

	pc_shutdown_logging();

	return 0;
}