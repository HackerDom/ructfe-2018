#pragma once

#include <queue>

#include "common.h"

template<typename TState>
struct connection;

struct node_state;
struct face_state;

template<typename TState>
struct response;

template<typename TState>
struct responder {
	int cmd_id;
	connection<TState> &conn;

	responder(int cmd_id, connection<TState> &conn) :
		cmd_id(cmd_id), conn(conn) { }

	void respond(const char *message) {
		conn.pending_commands.push(new response<TState>(message, cmd_id));
	}
};

template<typename TState>
struct command {
	char *text = NULL;
	int cmd_id = 0;

	command(const char *text, int cmd_id) : cmd_id(cmd_id) {
		if (text) {
			this->text = new char[strlen(text) + 1];
			strcpy(this->text, text);
		}
	}
	command() = default;
	virtual ~command(){
		delete[] text;
	}

	virtual const char *name() = 0;
	virtual bool needs_response() { return false; }

	virtual void execute(responder<TState> &rsp, connection<TState> &conn, TState &state) = 0;
	virtual bool handle_response(const char *response, TState &state) { return true; }
};

template<typename TState>
bool parse_command(char *str, command<TState> *&cmd);

template<typename TState>
struct response : command<TState> {
	using command<TState>::command;

	static const char *_name() { return "!"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<TState> &rsp, connection<TState> &conn, TState &state) {

		auto cmd = conn.executing_commands.find(this->cmd_id);
		if (cmd == conn.executing_commands.end()) {
			pc_log("Error: response::execute: there was no command with id %d waiting for a response.", this->cmd_id);
			return;
		}

		if (cmd->second->handle_response(this->text, conn.state)) {
			delete cmd->second;
			conn.executing_commands.erase(cmd);
		}
	}
};

template<typename TState>
struct test_command : command<TState> {
	using command<TState>::command;

	static const char *_name() { return "test"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<TState> &rsp, connection<TState> &conn, TState &state) {
		pc_log("test_command::execute: a test command was executed!");
	}
};

template<typename TState>
struct die_command : command<TState> {
	using command<TState>::command;

	static const char *_name() { return "die"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<TState> &rsp, connection<TState> &conn, TState &state) {
		pc_quit("Master told us to die.");
	}
};

template<typename TState>
struct end_command : command<TState> {
	using command<TState>::command;

	static const char *_name() { return "end"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<TState> &rsp, connection<TState> &conn, TState &state) {
		pc_log("end_command::execute: closing connection..");
		conn.close();
	}
};

template<typename TState>
struct say_command;

template<typename TState>
struct history_command : command<TState> {
	using command<TState>::command;

	static const char *_name() { return "history"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<TState> &rsp, connection<TState> &conn, TState &state) {
		rsp.respond("history line 1");
		rsp.respond("history line 2");
		rsp.respond("history line 3");
	}
};

template<typename TState>
struct connection {
	const addrinfo *addr = NULL;
	TState &state;

	std::queue<command<TState> *> pending_commands;
	std::unordered_map<int, command<TState> *> executing_commands;
	pc_connection conn;
	int cmd_id = 0;
	bool closed = false;

	connection(int socket, TState &state) : state(state) {
		pc_make_nonblocking(socket);
		conn = pc_connection(socket);
	}
	connection(const addrinfo &addr, TState &state) : state(state) {
		this->addr = &addr;
		reconnect();
	}
	void reconnect(){
		if (addr) {
			pc_log("connection::reconnect: connecting to remote endpoint..");
			pc_connect(*addr, conn);
		}
	}

	bool tick() {

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
				command<TState> *cmd;
				if (parse_command(conn.recv_buffer, cmd)) {

					responder<TState> rsp(cmd->cmd_id, *this);
					cmd->execute(rsp, *this, state);

					delete cmd;
				}
			}
		}

		if (!conn.is_sending() && !pending_commands.empty()) {
			command<TState> *cmd = pending_commands.front();
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

	template<typename T>
	int send(const char *text) {
		int id = cmd_id++;
		pending_commands.push(new T(text, id));
		return id;
	}

	void flush(int id) {
		tick();
		while (alive() && (conn.is_sending() || !pending_commands.empty() || executing_commands.find(id) != executing_commands.end()))
			tick();
	}

	bool alive() {
		return !closed && conn.alive;
	}
	void close() {
		closed = true;
	}
};

struct hb_daemon {
	bool master_available = false;
	time_t last_hb = 0;
	bool hb_sent = false;

	bool tick(connection<node_state> &conn);

	bool process_response(const char *response);
};

struct master_link {
	connection<node_state> master_conn;
	hb_daemon hb;

	master_link() = default;
	master_link(const addrinfo &master_addr, node_state &state) : master_conn(master_addr, state) { }

	void tick();
};

struct node_state {
	master_link uplink;
	void *history_storage;

	node_state() = default;
	node_state(const addrinfo &master_addr) : uplink(master_addr, *this) { }
};

struct face_state {

};

#define HB_PERIOD 3

struct hb_command : command<node_state> {
	using command<node_state>::command;

	static const char *_name() { return "hb"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) { }

	virtual bool needs_response() { return true; }

	virtual bool handle_response(const char *response, node_state &state) {
		state.uplink.hb.process_response(response);
	}
};

bool hb_daemon::tick(connection<node_state> &conn) {
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

template<>
struct say_command<node_state> : command<node_state> {
	using command<node_state>::command;

	static const char *_name() { return "say"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) {
		pc_log("say_command::execute: saying '%s'..", this->text);
		state.uplink.master_conn.send<say_command>(this->text);
		conn.flush(conn.send<say_command>(this->text));
	}
};

template<>
struct say_command<face_state> : command<face_state> {
	using command<face_state>::command;

	static const char *_name() { return "say"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder<face_state> &rsp, connection<face_state> &conn, face_state &state) {
		pc_log("say_command::execute: saying '%s' @ face..", this->text);
	}
};

#define COMMAND_CASE(x) \
	if (!strcmp(x::_name(), name_str)) { \
		cmd = new x(text_str, atoi(id_str)); \
		return true; \
	}

template<>
bool parse_command<node_state>(char *str, command<node_state> *&cmd) {

	pc_log("parse_command: '%s'", str);

	char *id_str = strtok(str, " ");
	char *name_str = strtok(NULL, " ");
	char *text_str = strtok(NULL, " ");

	if (!id_str || !name_str)
		return false;

	pc_log("parse_command: id: '%d', name: '%s', text: '%s'", atoi(id_str), name_str, text_str);

	COMMAND_CASE(test_command<node_state>)
	COMMAND_CASE(hb_command)
	COMMAND_CASE(die_command<node_state>)
	COMMAND_CASE(end_command<node_state>)
	COMMAND_CASE(say_command<node_state>)
	COMMAND_CASE(history_command<node_state>)
	COMMAND_CASE(response<node_state>)

	return false;
}

template<typename TState>
bool parse_command(char *str, command<TState> *&cmd) {

	pc_log("parse_command: '%s'", str);

	char *id_str = strtok(str, " ");
	char *name_str = strtok(NULL, " ");
	char *text_str = strtok(NULL, " ");

	if (!id_str || !name_str)
		return false;

	pc_log("parse_command: id: '%d', name: '%s', text: '%s'", atoi(id_str), name_str, text_str);

	COMMAND_CASE(test_command<TState>)
	COMMAND_CASE(die_command<TState>)
	COMMAND_CASE(end_command<TState>)
	COMMAND_CASE(say_command<TState>)
	COMMAND_CASE(history_command<TState>)
	COMMAND_CASE(response<TState>)

	return false;
}