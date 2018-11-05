#include "commands.h"

void responder::respond(const char *message) {
	conn.pending_commands.push(new response(message, cmd_id));
}

command::command(const char *text, int cmd_id) : cmd_id(cmd_id) {
	if (text) {
		this->text = new char[strlen(text) + 1];
		strcpy(this->text, text);
	}
};
command::~command() {
	delete[] text;
}

void response::execute(responder &rsp, connection &conn, void *state) {

	auto cmd = conn.executing_commands.find(cmd_id);
	if (cmd == conn.executing_commands.end()) {
		pc_log("Error: response::execute: there was no command with id %d waiting for a response.", cmd_id);
		return;
	}

	if (cmd->second->handle_response(text, conn.parent)) {
		delete cmd->second;
		conn.executing_commands.erase(cmd);
	}
}

void test_command::execute(responder &rsp, connection &conn, void *state) {
	pc_log("test_command::execute: a test command was executed!");
}

void die_command::execute(responder &rsp, connection &conn, void *state) {
	pc_quit("Master told us to die.");
}

connection::connection(int socket, void *parent) : parent(parent) {
	pc_make_nonblocking(socket);
	conn = pc_connection(socket);
}
connection::connection(const addrinfo &addr, void *parent) : parent(parent) {
	this->addr = &addr;
	reconnect();
}
void connection::reconnect() {
	if (addr) {
		pc_connect(*addr, conn);
	}
}
bool connection::tick() {
	if (!conn.is_receiving()) {
		conn.receive();
	}
	else {
		int result = conn.poll_receive();
		if (result < 0)
			return false;
		if (result) {
			command *cmd;
			if (pc_parse_command(conn.recv_buffer, cmd)) {

				responder rsp(cmd->cmd_id, *this);
				cmd->execute(rsp, *this, parent);

				delete cmd;
			}
		}
	}

	if (!conn.is_sending() && !pending_commands.empty()) {
		command *cmd = pending_commands.front();
		pending_commands.pop();

		conn.send("%d %s %s", cmd->cmd_id, cmd->name(), cmd->text);

		if (cmd->needs_response()) {
			executing_commands[cmd->cmd_id] = cmd;
			pc_log("connection::tick: saved a command with id %d..", cmd->cmd_id);
		}
		else
			delete cmd;
	}
	else if (conn.is_sending()) {
		int result = conn.poll_send();
		if (result < 0)
			return false;
	}

	return true;
}
#define HB_PERIOD 3

bool hb_daemon::tick(connection &conn) {
	if (!hb_sent && time(NULL) - last_hb >= HB_PERIOD) {
		conn.send<hb_command>("HB!");
		hb_sent = true;
	}
}

bool hb_daemon::process_response(const char *response) {
	pc_log("hb_daemon::process_response: received '%s'.", response);
	last_hb = time(NULL);
	master_available = true;
	hb_sent = false;
	return true;
}

void master_link::tick() {
	hb.tick(master_conn);
	master_conn.tick();
}

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

bool controller::tick() {
	conn.tick();
	pc_log("controller::tick: alive = %d. Addr of alive: %p", alive, &alive);
	return alive;
}

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

void say_command::execute(responder &rsp, connection &conn, void *state) {
	// TODO relay it to master
}