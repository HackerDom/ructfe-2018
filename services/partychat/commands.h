#pragma once

#include <queue>

#include "common.h"

// Infrastructure

	template<typename TState>
	struct connection;

	struct node_state;
	struct face_state;
	struct checker_state;

	template<typename TState>
	struct response;
	template<typename TState>
	struct end_command;

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
		void reconnect() {
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
			send<end_command<TState>>("bye");
			for (int i = 0; i < 10; i++)
				tick();
			closed = true;
		}
	};

// Commands (generic)

	template<typename TState>
	struct die_command;

	template<typename TState>
	struct end_command;

	template<typename TState>
	struct hb_command;

	template<typename TState>
	struct say_command;

	template<typename TState>
	struct history_command;

	template<typename TState>
	struct list_command;

// State

	#define HB_PERIOD 3

	struct hb_daemon {
		bool master_available = false;
		time_t last_hb = 0;
		bool hb_sent = false;
		const char *nick;

		hb_daemon(const char *nick) : nick(nick) { }

		bool tick(connection<node_state> &conn) {
			if (!hb_sent && time(NULL) - last_hb >= HB_PERIOD) {
				conn.send<hb_command<node_state>>(nick);
				hb_sent = true;
			}
		}

		bool process_response(const char *response) {
			//pc_log("hb_daemon::process_response: received '%s'.", response);
			last_hb = time(NULL);
			master_available = true;
			hb_sent = false;
			return true;
		}
	};

	struct master_link {
		connection<node_state> master_conn;
		hb_daemon hb;

		master_link(const addrinfo &master_addr, node_state &state, const char *nick) : master_conn(master_addr, state), hb(nick) { }

		void tick() {
			hb.tick(master_conn);
			master_conn.tick();
		}
	};

	#define CON_CT 8

	struct node_state {
		master_link uplink;
		connection<node_state> *controllers[CON_CT];
		int list_id = -1;

		node_state(const addrinfo &master_addr, const char *nick) : uplink(master_addr, *this, nick) {
			bzero(controllers, sizeof(controllers));
		}

		~node_state() {
			for (int i = 0; i < CON_CT; i++) {
				delete controllers[i];
			} 
		}
	};

	struct face_state {

	};

	struct checker_state {
		const char *team_nick;
		bool team_listed = false;

		const char *flag;
		bool flag_found = false;

		checker_state(const char *team_nick) : team_nick(team_nick), flag(NULL) { }

		checker_state(const char *team_nick, const char *flag) : team_nick(team_nick), flag(flag) { }
	};

// Commands (specialized)

	template<>
	struct die_command<checker_state> : command<checker_state> {
		using command<checker_state>::command;

		static const char *_name() { return "die"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<checker_state> &rsp, connection<checker_state> &conn, checker_state &state) {
			pc_log("die_command::execute: closing connection..");
			conn.close();
		}
	};

	template<>
	struct end_command<checker_state> : command<checker_state> {
		using command<checker_state>::command;

		static const char *_name() { return "end"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<checker_state> &rsp, connection<checker_state> &conn, checker_state &state) {
			pc_log("end_command::execute: closing connection..");
			conn.close();
		}
	};

	template<>
	struct die_command<node_state> : command<node_state> {
		using command<node_state>::command;

		static const char *_name() { return "die"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) {
			conn.close();
			pc_quit("Master told us to die.");
		}
	};

	template<>
	struct end_command<node_state> : command<node_state> {
		using command<node_state>::command;

		static const char *_name() { return "end"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) {
			pc_log("end_command::execute: closing connection..");
			conn.close();
		}
	};

	template<>
	struct die_command<face_state> : command<face_state> {
		using command<face_state>::command;

		static const char *_name() { return "die"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<face_state> &rsp, connection<face_state> &conn, face_state &state) {
			conn.close();
			pc_quit("Node told us to die.");
		}
	};

	template<>
	struct end_command<face_state> : command<face_state> {
		using command<face_state>::command;

		static const char *_name() { return "end"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<face_state> &rsp, connection<face_state> &conn, face_state &state) {
			pc_log("end_command::execute: closing connection..");
			conn.close();
		}
	};

	template<>
	struct hb_command<node_state> : command<node_state> {
		using command<node_state>::command;

		static const char *_name() { return "hb"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) { }

		virtual bool needs_response() { return true; }

		virtual bool handle_response(const char *response, node_state &state) {
			state.uplink.hb.process_response(response);
			return true;
		}
	};

	template<>
	struct hb_command<checker_state> : command<checker_state> {
		using command<checker_state>::command;

		static const char *_name() { return "hb"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<checker_state> &rsp, connection<checker_state> &conn, checker_state &state) { }
		virtual bool needs_response() { return true; }
	};

	template<>
	struct say_command<node_state> : command<node_state> {
		using command<node_state>::command;

		static const char *_name() { return "say"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) {
			if (&conn == &state.uplink.master_conn) {
				//pc_log("say_command::execute: saying '%s' to controllers..", this->text);
				pc_group g(this->text);

				for (int i = 0; i < CON_CT; i++) {
					if (state.controllers[i]) {
						state.controllers[i]->send<say_command>(this->text);
					}
				}
				pc_add_line(g, this->text);
			}
			else {
				//pc_log("say_command::execute: saying '%s'..", this->text);
				pc_group g(this->text);

				char buffer[CONN_BUFFER_LENGTH];
				snprintf(buffer, sizeof(buffer), "%s says: %s", state.uplink.hb.nick, this->text);
				state.uplink.master_conn.send<say_command>(buffer);
			}
		}
	};

	template<>
	struct say_command<face_state> : command<face_state> {
		using command<face_state>::command;

		static const char *_name() { return "say"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<face_state> &rsp, connection<face_state> &conn, face_state &state) {
			//pc_log("say_command::execute: saying '%s'..", this->text);
			printf(": %s\n", this->text);
		}
	};


	template<>
	struct say_command<checker_state> : command<checker_state> {
		using command<checker_state>::command;

		static const char *_name() { return "say"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<checker_state> &rsp, connection<checker_state> &conn, checker_state &state) { }
	};

	template<>
	struct history_command<node_state> : command<node_state> {
		using command<node_state>::command;

		static const char *_name() { return "history"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) {
			if (&conn == &state.uplink.master_conn) {
				//pc_log("history_command::execute: loading history for '%s'..", this->text);
				pc_group g(this->text);

				pc_send_lines(g, [&rsp](const char *line) { rsp.respond(line); });
				rsp.respond("");
			}
			else {
				//pc_log("history_command::execute: getting history from master '%s'..", this->text);
				pc_group g(this->text);

				state.uplink.master_conn.send<history_command>(this->text);
			}
		}

		virtual bool needs_response() { return true; }

		virtual bool handle_response(const char *response, node_state &state) {
			//pc_log("history_command::handle_response: %s", response);
			if (!response || strlen(response) == 0)
				return true;

			for (int i = 0; i < CON_CT; i++) {
				if (state.controllers[i])
					state.controllers[i]->send<history_command>(this->text);
			}
			return false;
		}
	};

	template<>
	struct history_command<face_state> : command<face_state> {
		using command<face_state>::command;

		static const char *_name() { return "history"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<face_state> &rsp, connection<face_state> &conn, face_state &state) {
			printf(": %s\n", this->text);
		}
	};

	template<>
	struct history_command<checker_state> : command<checker_state> {
		using command<checker_state>::command;

		static const char *_name() { return "history"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<checker_state> &rsp, connection<checker_state> &conn, checker_state &state) {
			rsp.respond("");
		}

		virtual bool needs_response() { return true; }

		virtual bool handle_response(const char *response, checker_state &state) {
			//pc_log("history_command::handle_response: %s", response);
			if (!response || strlen(response) == 0)
				return true;

			if (strstr(response, state.flag)) {
				state.flag_found = true;
				return true;
			}

			return false;
		}
	};

	template<>
	struct list_command<checker_state> : command<checker_state> {
		using command<checker_state>::command;

		static const char *_name() { return "list"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<checker_state> &rsp, connection<checker_state> &conn, checker_state &state) { }

		virtual bool needs_response() { return true; }

		virtual bool handle_response(const char *response, checker_state &state) {
			//pc_log("list_command::handle_response: %s", response);
			if (!response || strlen(response) == 0)
				return true;

			if (!strcmp(response, state.team_nick)) {
				state.team_listed = true;
				return true;
			}

			return false;
		}
	};

	template<>
	struct list_command<face_state> : command<face_state> {
		using command<face_state>::command;

		static const char *_name() { return "list"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<face_state> &rsp, connection<face_state> &conn, face_state &state) { }

		virtual bool needs_response() { return true; }

		virtual bool handle_response(const char *response, face_state &state) {
			if (!response || strlen(response) == 0)
				return true;

			printf("- %s\n", response);
			return false;
		}
	};

	template<>
	struct list_command<node_state> : command<node_state> {
		using command<node_state>::command;

		static const char *_name() { return "list"; }
		virtual const char *name() { return _name(); }

		virtual void execute(responder<node_state> &rsp, connection<node_state> &conn, node_state &state) {
			state.list_id = cmd_id;
			state.uplink.master_conn.send<list_command>(this->text);
		}

		virtual bool needs_response() { return true; }

		virtual bool handle_response(const char *rsp, node_state &state) {
			if (state.list_id < 0)
				return true;
			if (!rsp || strlen(rsp) == 0) {

				for (int i = 0; i < CON_CT; i++) {
					if (state.controllers[i])
						state.controllers[i]->pending_commands.push(new response<node_state>("", state.list_id));
				}
				return true;
			}

			for (int i = 0; i < CON_CT; i++) {
				if (state.controllers[i])
					state.controllers[i]->pending_commands.push(new response<node_state>(rsp, state.list_id));
			}
			return false;
		}
	};


// Parse commands

	#define COMMAND_CASE(x) \
		if (!strcmp(x::_name(), name_str)) { \
			cmd = new x(text_str, atoi(id_str)); \
			return true; \
		}

	template<typename TState>
	bool parse_command(char *str, command<TState> *&cmd) {

		//pc_log("parse_command: '%s'", str);

		char *id_str = strtok(str, " ");
		char *name_str = strtok(NULL, " ");

		if (!id_str || !name_str)
			return false;

		char *text_str = name_str + strlen(name_str) + 1;

		//pc_log("parse_command: id: '%d', name: '%s', text: '%s'", atoi(id_str), name_str, text_str);

		COMMAND_CASE(die_command<TState>)
		COMMAND_CASE(end_command<TState>)
		COMMAND_CASE(say_command<TState>)
		COMMAND_CASE(history_command<TState>)
		COMMAND_CASE(list_command<TState>)
		COMMAND_CASE(response<TState>)

		return false;
	}
