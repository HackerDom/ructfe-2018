#pragma once

#include <queue>
#include <unordered_map>

#include "common.h"

struct connection;

struct responder {
	int cmd_id;
	connection &conn;

	responder(int cmd_id, connection &conn) :
		cmd_id(cmd_id), conn(conn) { }

	void respond(const char *message);
};

struct command {
	char *text = NULL;
	int cmd_id = 0;

	command(const char *text, int cmd_id);
	command() = default;
	virtual ~command();

	virtual const char *name() = 0;
	virtual bool needs_response() { return false; }

	virtual void execute(responder &rsp, connection &conn, void *state) = 0;
	virtual bool handle_response(const char *response, void *state) { return true; }
};

struct response : command {
	using command::command;

	static const char *_name() { return "!"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, void *state);
};

struct test_command : command {
	using command::command;

	static const char *_name() { return "test"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, void *state);
};

struct hb_command;

struct end_command;

struct die_command : command {
	using command::command;

	static const char *_name() { return "die"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, void *state);
};

struct connection {
	const addrinfo *addr = NULL;
	void *parent = NULL;

	std::queue<command *> pending_commands;
	std::unordered_map<int, command *> executing_commands;
	pc_connection conn;
	int cmd_id = 0;

	connection() = default;
	connection(int socket, void *parent);
	connection(const addrinfo &addr, void *parent);
	void reconnect();

	bool tick();

	template<typename T>
	void send(const char *text) {
		pending_commands.push(new T(text, cmd_id++));
	}
};

bool pc_parse_command(char *str, command *&cmd);

struct controller {
	connection conn;
	bool alive = true;

	controller() = default;
	controller(int socket) : conn(socket, this) { }

	bool tick();
};

struct hb_daemon {
	bool master_available = false;
	time_t last_hb = 0;
	bool hb_sent = false;

	bool tick(connection &conn);

	bool process_response(const char *response);
};

struct master_link {
	connection master_conn;
	hb_daemon hb;

	master_link(const addrinfo &master_addr) : master_conn(master_addr, this) { }

	void tick();
};

struct say_command : command {
	using command::command;

	static const char *_name() { return "say"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, void *state);
};