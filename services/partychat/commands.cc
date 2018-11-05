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