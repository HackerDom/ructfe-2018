#include <time.h>

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

	char log_file[64];
	sprintf(log_file, "%s.log", argv[0]);
	pc_init_logging(log_file, true);

	pc_log("Running with args: %s:%d %d", 
		inet_ntoa(((sockaddr_in *)master_address->ai_addr)->sin_addr), 
		ntohs(((sockaddr_in *)master_address->ai_addr)->sin_port), 
		control_port);

	hb_daemon hb(*master_address);
	while (true) {
		hb.tick();
		pc_log("Master: %s, state: %d", hb.master_available ? "available" : "unavailable", hb.state);
	}

	pc_shutdown_logging();

	return 0;
}