#pragma once

#include <queue>

#include "common.h"

struct connection;
struct node_state;

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

	virtual void execute(responder &rsp, connection &conn, node_state &state) = 0;
	virtual bool handle_response(const char *response, node_state &state) { return true; }
};

struct response : command {
	using command::command;

	static const char *_name() { return "!"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state);
};

struct test_command : command {
	using command::command;

	static const char *_name() { return "test"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state);
};

struct die_command : command {
	using command::command;

	static const char *_name() { return "die"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state);
};

struct end_command : command {
	using command::command;

	static const char *_name() { return "end"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state);
};

struct say_command : command {
	using command::command;

	static const char *_name() { return "say"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state);
};

struct history_command : command {
	using command::command;

	static const char *_name() { return "history"; }
	virtual const char *name() { return _name(); }

	virtual void execute(responder &rsp, connection &conn, node_state &state);
};

struct connection {
	const addrinfo *addr = NULL;
	node_state &state;

	std::queue<command *> pending_commands;
	std::unordered_map<int, command *> executing_commands;
	pc_connection conn;
	int cmd_id = 0;
	bool closed = false;

	connection();
	connection(int socket, node_state &state);
	connection(const addrinfo &addr, node_state &state);
	connection(const addrinfo &addr);
	void reconnect();

	bool tick();

	template<typename T>
	int send(const char *text) {
		int id = cmd_id++;
		pending_commands.push(new T(text, id));
		return id;
	}

	void flush(int id);

	bool alive();
	void close();
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

	master_link() = default;
	master_link(const addrinfo &master_addr, node_state &state) : master_conn(master_addr, state) { }

	void tick();
};

struct node_state {
	master_link uplink;
	void *history_storage;

	node_state() = default;
	node_state(const addrinfo &master_addr) : uplink(master_addr, *this) { }

	static node_state _default;
};