#include "commands.h"

bool parse_command(char *str, command *&cmd);

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

void response::execute(responder &rsp, connection &conn, node_state &state) {

	auto cmd = conn.executing_commands.find(cmd_id);
	if (cmd == conn.executing_commands.end()) {
		pc_log("Error: response::execute: there was no command with id %d waiting for a response.", cmd_id);
		return;
	}

	if (cmd->second->handle_response(text, conn.state)) {
		delete cmd->second;
		conn.executing_commands.erase(cmd);
	}
}

void test_command::execute(responder &rsp, connection &conn, node_state &state) {
	pc_log("test_command::execute: a test command was executed!");
}

void die_command::execute(responder &rsp, connection &conn, node_state &state) {
	pc_quit("Master told us to die.");
}

connection::connection() : state(node_state::_default) { }
connection::connection(int socket, node_state &state) : state(state) {
	pc_make_nonblocking(socket);
	conn = pc_connection(socket);
}
connection::connection(const addrinfo &addr, node_state &state) : state(state) {
	this->addr = &addr;
	reconnect();
}
connection::connection(const addrinfo &addr) : state(node_state::_default) {
	this->addr = &addr;
	reconnect();
}
void connection::reconnect() {
	if (addr) {
		pc_log("connection::reconnect: connecting to remote endpoint..");
		pc_connect(*addr, conn);
	}
}
bool connection::tick() {
	if (closed)
		return false;

	if (!conn.alive)
		reconnect();

	if (!conn.is_receiving()) {
		conn.receive();
	}
	else {
		int result = conn.poll_receive();
		if (result < 0)
			return false;
		if (result) {
			command *cmd;
			if (parse_command(conn.recv_buffer, cmd)) {

				responder rsp(cmd->cmd_id, *this);
				cmd->execute(rsp, *this, state);

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
void connection::flush(int id) {
	tick();
	while (alive() && (conn.is_sending() || !pending_commands.empty() || executing_commands.find(id) != executing_commands.end()))
		tick();
}
bool connection::alive() {
	return !closed && conn.alive;
}
void connection::close() {
	closed = true;
}

#define HB_PERIOD 3

struct hb_command : command {
	using command::command;

	static const char *_name() { return "hb"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state) { }

	virtual bool needs_response() { return true; }

	virtual bool handle_response(const char *response, node_state &state) {
		state.uplink.hb.process_response(response);
	}
};

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

node_state node_state::_default;

void end_command::execute(responder &rsp, connection &conn, node_state &state) {
	pc_log("end_command::execute: closing connection..");
	conn.close();
}

#define COMMAND_CASE(x) \
	if (!strcmp(x::_name(), name_str)) { \
		cmd = new x(text_str, atoi(id_str)); \
		return true; \
	}

bool parse_command(char *str, command *&cmd) {

	pc_log("parse_command: '%s'", str);

	char *id_str = strtok(str, " ");
	char *name_str = strtok(NULL, " ");
	char *text_str = strtok(NULL, " ");

	if (!id_str || !name_str)
		return false;

	pc_log("parse_command: id: '%d', name: '%s', text: '%s'", atoi(id_str), name_str, text_str);

	COMMAND_CASE(test_command)
	COMMAND_CASE(hb_command)
	COMMAND_CASE(die_command)
	COMMAND_CASE(end_command)
	COMMAND_CASE(say_command)
	COMMAND_CASE(history_command)
	COMMAND_CASE(response)

	return false;
}

void say_command::execute(responder &rsp, connection &conn, node_state &state) {
	pc_log("say_command::execute: saying '%s'..", text);
	state.uplink.master_conn.send<say_command>(text);
}

void history_command::execute(responder &rsp, connection &conn, node_state &state) {
	rsp.respond("history line 1");
	rsp.respond("history line 2");
	rsp.respond("history line 3");
}

