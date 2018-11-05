#include <time.h>
#include <poll.h>

#include "common.h"
#include "commands.h"

typedef unsigned short ushort;

#define HB_PERIOD 3

struct hb_daemon {
	bool master_available = false;
	time_t last_hb = 0;
	bool hb_sent = false;

	bool tick(connection &conn) {
		if (!hb_sent && time(NULL) - last_hb >= HB_PERIOD) {
			conn.send<hb_command>("HB!");
			hb_sent = true;
		}
	}

	bool process_response(const char *response) {
		pc_log("hb_daemon::process_response: received '%s'.", response);
		last_hb = time(NULL);
		master_available = true;
		hb_sent = false;
		return true;
	}
};

struct master_link {
	connection master_conn;
	hb_daemon hb;

	master_link(const addrinfo &master_addr) : master_conn(master_addr, this) { }

	void tick() {
		hb.tick(master_conn);
		master_conn.tick();
	}
};

struct hb_command : command {
	using command::command;

	static const char *_name() { return "hb"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, void *state) { }

	virtual bool needs_response() { return true; }

	virtual bool handle_response(const char *response, void *state) {
		master_link *link = static_cast<master_link *>(state);
		link->hb.process_response(response);
	}
};

struct controller {
	connection conn;
	bool alive = true;

	controller() = default;
	controller(int socket) : conn(socket, this) { }

	bool tick() {
		conn.tick();
		pc_log("controller::tick: alive = %d. Addr of alive: %p", alive, &alive);
		return alive;
	}
};

struct end_command : command {
	using command::command;

	static const char *_name() { return "end"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, void *state) {
		controller *c = static_cast<controller *>(state);
		c->alive = false;
		pc_log("end_command::execute: controller is dead, haha! Addr of c->alive: %p", &c->alive);
	}
};

#define COMMAND_CASE(x) \
	if (!strcmp(x::_name(), name_str)) { \
		cmd = new x(text_str, atoi(id_str)); \
		return true; \
	}

bool pc_parse_command(char *str, command *&cmd) {

	pc_log("pc_parse_command: '%s'", str);

	char *id_str = strtok(str, " ");
	char *name_str = strtok(NULL, " ");
	char *text_str = strtok(NULL, " ");

	if (!id_str || !name_str)
		return false;

	pc_log("pc_parse_command: id: '%d', name: '%s', text: '%s'", atoi(id_str), name_str, text_str);

	COMMAND_CASE(test_command)
	COMMAND_CASE(hb_command)
	COMMAND_CASE(die_command)
	COMMAND_CASE(end_command)
	COMMAND_CASE(response)

	return false;
}

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