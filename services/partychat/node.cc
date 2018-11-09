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

#define SRV_CT 2
#define CON_CT 8

int main(int argc, char **argv) {

	addrinfo *master_address;
	ushort control_port;

	if (argc != 4 || !parse_args(argv[1], argv[2], &master_address, &control_port)) {
		printf("Usage:\n");
		printf("%s <master_endpoint> <control_port> <nick_name>\n", argv[0]);
		exit(-1);
	}
	const char *nick = argv[3];

	char log_file[64];
	sprintf(log_file, "%s.log", argv[0]);
	pc_init_logging(log_file, true);

	pc_log("Running with args: %s:%d %d %s", 
		inet_ntoa(((sockaddr_in *)master_address->ai_addr)->sin_addr), 
		ntohs(((sockaddr_in *)master_address->ai_addr)->sin_port), 
		control_port,
		nick);

	node_state state(*master_address, nick);

	int server_sock = pc_start_server(control_port);

	pollfd fds[SRV_CT + CON_CT];
	memset(&fds, 0, sizeof(fds));
	int used_fds = SRV_CT + CON_CT;

	fds[0].fd = server_sock;
	fds[0].events = POLLIN;

	while (true) {

		state.uplink.tick();

		pc_log("Master: %s", state.uplink.hb.master_available ? "available" : "unavailable");

		if (!state.uplink.master_conn.alive()) {
			sleep(1);
			continue;
		}

		fds[1].fd = state.uplink.master_conn.conn.socket;
		fds[1].events = POLLIN;

		int result = poll(fds, used_fds, 1000);
		if (result < 0)
			pc_fatal("main: poll() failed unexpectedly.");

		for (int i = 0; i < used_fds; i++) {

			if (i == 0 && fds[0].revents == POLLIN) {
				int client_idx = find_empty_slot(fds + SRV_CT, CON_CT);
				
				if (client_idx >= 0) {

					int client_sock = pc_accept_client(server_sock);
					if (client_sock) {
						int fd_idx = client_idx + SRV_CT;

						state.controllers[client_idx] = new connection<node_state>(client_sock, state);
						fds[fd_idx].fd = client_sock;
						fds[fd_idx].events = POLLIN;
					}
					pc_log("main: accepted a new controlling connection.");
				}
				else {
					pc_log("main: cannot accept more controlling connections.");
					fds[0].events = 0;
				}
				continue;
			}
			if (i < SRV_CT)
				continue;

			int con_idx = i - SRV_CT;
			if (!state.controllers[con_idx])
				continue;
			if (!state.controllers[con_idx]->tick()) {
				delete state.controllers[con_idx];
				state.controllers[con_idx] = NULL;
				bzero(&fds[i], sizeof(pollfd));
				fds[0].events = POLLIN;
			}
		}
	}

	pc_shutdown_logging();

	return 0;
}