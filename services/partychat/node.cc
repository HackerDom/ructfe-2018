#include <time.h>
#include <poll.h>

#include "common.h"

typedef unsigned short ushort;

#define HB_IDLE 0
#define HB_SEND 1
#define HB_RECV 2

#define HB_PERIOD 3

struct hb_daemon {
	const addrinfo &master_addr;
	pc_connection master_conn;
	bool master_available = false;
	int state = HB_IDLE;
	time_t last_hb = 0;

	hb_daemon(const addrinfo &master_addr) : master_addr(master_addr) {
		pc_connect(master_addr, master_conn);
	}

	void tick() {
		int result;

		switch (state) {

			case HB_IDLE:

				if (time(NULL) - last_hb >= HB_PERIOD) {
					if (master_conn.alive) {
						master_conn.send("HB!");
						state = HB_SEND;
					} 
					else {
						pc_log("hb_daemon: reconnecting to master..");
						pc_connect(master_addr, master_conn);
						master_available = false;
					}
				}
				break;

			case HB_SEND:

				result = master_conn.poll_send();
				if (result < 0) {
					state = HB_IDLE;
					master_available = false;
				} 
				else if (result) {
					master_conn.receive();
					state = HB_RECV;
				}
				break;

			case HB_RECV:

				result = master_conn.poll_receive();
				if (result < 0) {
					state = HB_IDLE;
					master_available = false;
				}
				else if (result) {
					master_available = process_response(master_conn.recv_buffer);
					state = HB_IDLE;
					last_hb = time(NULL);
				}
				break;
		}
	}

	bool process_response(const char *response) {
		pc_log("process_response: received '%s'", response);
		if (!strcmp(response, "q"))
			return false;
		return true;
	}
};

#define CT_IDLE 0
#define CT_RECV 1
#define CT_SEND 2

struct controller {
	pc_connection conn;
	int state = CT_IDLE;

	controller() = default;
	controller(int socket) {
		conn = pc_connection(socket);
	}

	bool tick() {
		int result;
		pc_log("controller: tick!");

		switch (state) {

			case CT_IDLE:

				conn.receive();
				state = CT_RECV;
				break;

			case CT_RECV:

				result = conn.poll_receive();
				if (result < 0) {
					pc_log("Error: a control connection was closed unexpectedly.");
					return false;
				}
				else if (result) {
					pc_log("Received command: '%s'.", conn.recv_buffer);
					state = CT_IDLE;
				}
				break;
		}

		return true;
	}
};

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

	int server_sock = pc_start_server(control_port);

	pollfd fds[FDS_CT + 1];
	memset(&fds, 0, sizeof(fds));
	int used_fds = FDS_CT + 1;

	fds[0].fd = server_sock;
	fds[0].events = POLLIN;

	hb_daemon hb(*master_address);
	controller controllers[FDS_CT];

	while (true) {

		hb.tick();
		pc_log("Master: %s, state: %d", hb.master_available ? "available" : "unavailable", hb.state);

		int result = poll(fds, used_fds, 100);
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
						controllers[client_idx] = controller(client_sock);
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

			if (!controllers[i - 1].tick()) {
				controllers[i - 1].~controller();
				bzero(&fds[i], sizeof(pollfd));
				fds[0].events = POLLIN;
			}
		}
	}

	pc_shutdown_logging();

	return 0;
}