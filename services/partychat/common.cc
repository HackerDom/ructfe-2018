#include <stdarg.h>
#include <time.h>
#include <vector>
#include <algorithm>
#include <sys/stat.h>

#include "common.h"

// Logging

	#define LOG_BUFFER_SIZE 256

	FILE *pc_log_stream;
	bool pc_log_echo;

	void pc_init_logging(const char *file, bool echo) {
		pc_log_stream = fopen(file, "w");

		if (!pc_log_stream)
			pc_fatal("Failed to open log stream to file '%s'.", file);

		pc_log_echo = echo;
	}

	void pc_shutdown_logging() {
		if (pc_log_stream)
			fclose(pc_log_stream);
	}

	void pc_log(const char *format, ...) {

		char message[LOG_BUFFER_SIZE];

		va_list args;
		va_start(args, format);
		vsnprintf(message, sizeof(message), format, args);
		va_end(args);

		char buffer[26];
		struct tm* tm_info;
		time_t timer;

		if (!pc_log_stream)
			return;

		time(&timer);
		tm_info = localtime(&timer);

		strftime(buffer, 26, "%Y-%m-%d %H:%M:%S", tm_info);

		fprintf(pc_log_stream, "%s %s\n", buffer, message);
		fflush(pc_log_stream);

		if (pc_log_echo)
			printf("%s %s\n", buffer, message);
	}

	void pc_fatal(const char *format, ...) {

		char message[LOG_BUFFER_SIZE];

		va_list args;
		va_start(args, format);
		vsnprintf(message, sizeof(message), format, args);
		va_end(args);

		pc_shutdown_logging();

		printf("Fatal: %s\n", message);
		exit(-1);
	}

	void pc_quit(const char *format, ...) {

		char message[LOG_BUFFER_SIZE];

		va_list args;
		va_start(args, format);
		vsnprintf(message, sizeof(message), format, args);
		va_end(args);

		pc_shutdown_logging();

		printf("Terminating: %s\n", message);
		exit(0);
	}

// Networking

	pc_connection::pc_connection(int sock) {
		socket = sock;
		recv_buffer = new char[CONN_BUFFER_LENGTH];
		recv_leftover = new char[CONN_BUFFER_LENGTH];
		send_buffer = new char[CONN_BUFFER_LENGTH];
		alive = true;
		bzero(recv_leftover, CONN_BUFFER_LENGTH);
	}

	pc_connection::~pc_connection() {
		if (socket) {
			close(socket);
			delete[] recv_buffer;
			delete[] recv_leftover;
			delete[] send_buffer;
		}
	}
	
	pc_connection &pc_connection::operator=(pc_connection &&other) {
		memcpy(this, &other, sizeof(pc_connection));
		other.socket = 0;
		return *this;
	}

	void pc_connection::send(const char *format, ...) {
		if (!alive)
			return;

		if (send_length != 0)
			pc_fatal("pc_connection::send: previous send operation has not completed.");

		va_list args;
		va_start(args, format);
		if (vsnprintf(send_buffer, CONN_BUFFER_LENGTH - 1, format, args) > CONN_BUFFER_LENGTH - 1)
			pc_log("Error: pc_connection::send: message was truncated.");
		va_end(args);

		int message_length = strlen(send_buffer) + 1;
		strcat(send_buffer, "\n");

		send_length = message_length;
	}

	int pc_connection::poll_send() {
		if (send_length == 0)
			pc_fatal("pc_connection::poll_send: there is no active send operation.");

		int result = write(socket, send_buffer + send_index, send_length - send_index);
		if (result < 0) {
			if (errno == EWOULDBLOCK || errno == EAGAIN)
				return 0;

			pc_log("Error: poll_send: errno = %d.", result, errno);
			alive = false;
			return -1;
		}
		if (result == 0) {
			pc_log("Error: poll_send: connection was closed.");
			alive = false;
			return -1;
		}

		send_index += result;
		if (send_index == send_length) {
			send_length = send_index = 0;
			return 1;
		}

		return 0;
	}

	bool pc_connection::is_sending() {
		return send_length != 0;
	}

	void pc_connection::receive() {
		if (!alive)
			return;
		
		if (recv_length != 0) {
			pc_fatal("pc_connection::receive: previous receive operation has not completed.");
		}

		recv_length = CONN_BUFFER_LENGTH - 1;
	}

	int pc_connection::poll_receive() {
		if (recv_length == 0)
			pc_fatal("pc_connection::poll_receive: there is no active receive operation.");

		if (recv_leftover[0]) {
			strcpy(recv_buffer, recv_leftover);
			bzero(recv_leftover, CONN_BUFFER_LENGTH);

			recv_index = strlen(recv_buffer);
		}
		else {

			int result = read(socket, recv_buffer + recv_index, recv_length - recv_index);
			if (result < 0) {
				if (errno == EWOULDBLOCK || errno == EAGAIN)
					return 0;

				pc_log("Error: poll_receive: errno = %d.", result, errno);
				alive = false;
				return -1;
			}
			if (result == 0) {
				pc_log("Error: poll_receive: connection was closed.");
				alive = false;
				return -1;
			}

			recv_index += result;
		}

		recv_buffer[recv_index] = 0;
		char *endl = strchr(recv_buffer, '\n');
		if (endl) {
			*endl = 0;
			if (recv_buffer + recv_index > endl + 1) {
				strcpy(recv_leftover, endl + 1);
			}
			recv_length = recv_index = 0;
			return 1;
		}

		if (recv_index == recv_length) {
			pc_log("Error: pc_connection::poll_receive: received message was too long.");
			alive = false;
			return -1;
		}

		return 0;
	}

	bool pc_connection::is_receiving() {
		return recv_length != 0;
	}

	bool pc_connect(const addrinfo &endpoint, pc_connection &connection) {
		int sock = socket(AF_INET, SOCK_STREAM, 0);
		if (sock < 0)
			pc_fatal("pc_connect: failed to create socket.");

		if (connect(sock, endpoint.ai_addr, sizeof(*endpoint.ai_addr)) < 0) {
			pc_log("Error: pc_connect: failed to connect to remote endpoint.");
			close(sock);
			return false;
		}

		pc_make_nonblocking(sock);

		connection = pc_connection(sock);
		return true;
	}

	void pc_make_nonblocking(int socket) {
		int flags;
		if ((flags = fcntl(socket, F_GETFL, 0)) < 0)
			pc_fatal("pc_make_nonblocking: failed to get socket flags.");
		if (fcntl(socket, F_SETFL, flags | O_NONBLOCK) < 0)
			pc_fatal("pc_make_nonblocking: failed to set socket flags.");
	}

	bool pc_parse_endpoint(const char *str, addrinfo **endpoint) {

		char buffer[256];
		if (strlen(str) >= sizeof(buffer))
			return false;
		strcpy(buffer, str);

		char *ip_str = strtok(buffer, ":");
		char *port_str = strtok(NULL, ":");

		if (ip_str == NULL || port_str == NULL)
			return false;

		if (getaddrinfo(ip_str, port_str, NULL, endpoint) != 0)
			return false;

		return true;
	}

	int pc_start_server(int port) {

		int server_sock = socket(AF_INET, SOCK_STREAM, 0);
		if (server_sock < 0) 
			pc_fatal("pc_start_server: failed to create socket.");

		int reuse = 1;
		setsockopt(server_sock, SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(int));

		sockaddr_in server_addr;
		bzero(&server_addr, sizeof(server_addr));

		server_addr.sin_family = AF_INET;
		server_addr.sin_port = htons(port);

		if (bind(server_sock, (sockaddr *)&server_addr, sizeof(server_addr)) < 0) 
			pc_fatal("pc_start_server: failed to bind server socket.");

		pc_make_nonblocking(server_sock);

		if (listen(server_sock, 8) < 0)
			pc_fatal("pc_start_server: failed to listen on server socket.");

		return server_sock;
	}

	int pc_accept_client(int server_sock) {

		sockaddr_in client_addr;
		socklen_t client_len = sizeof(client_addr);

		int client_sock = accept(server_sock, (sockaddr *)&client_addr, &client_len);
		if (client_sock < 0) {
			pc_log("Error: pc_accept_client: failed to accept client.");
			return 0;
		}

		pc_make_nonblocking(client_sock);
		return client_sock;
	}

// Groups

	char *pc_extract_group(const char *message) {

		if (!message)
			return NULL;

		if (strlen(message) > CONN_BUFFER_LENGTH) {
			pc_log("Error: pc_extract_group: message was too long.");
			return NULL;
		}

		char buffer[CONN_BUFFER_LENGTH];
		strcpy(buffer, message);

		std::vector<char *> names;

		char *token = strtok(buffer, " ");
		while (token) {

			if (token[0] == '@')
				names.push_back(token);
			token = strtok(NULL, " ");
		}
		if (names.empty())
			return NULL;

		std::sort(names.begin(), names.end(), [](const char *a, const char *b) { return strcmp(a, b) < 0; });

		std::vector<char *> unique_names;
		unique_names.push_back(names[0]);
		for (int i = 1; i < names.size(); i++) {
			if (!strcmp(names[i], names[i - 1]))
				continue;
			unique_names.push_back(names[i]);
		}

		int length = 0;
		for (auto s : unique_names) {
			length += 1 + strlen(s);
		}

		char *g = new char[length + 1];
		bzero(g, length + 1);
		for (auto s : unique_names) {
			strcat(g, s);
			strcat(g, " ");
		}
		g[length - 1] = 0;

		return g;
	}

	pc_group::pc_group(const char *message) {
		group = pc_extract_group(message);
	}
	pc_group::~pc_group() {
		delete[] group;
	}
	bool pc_group::operator==(const pc_group &other) const {
		if (!group && !other.group)
			return true;
		if (!group || !other.group)
			return false;
		return !strcmp(group, other.group);
	}


// Storage

	#define HIST_MAX 50

	char large_buffer[HIST_MAX * CONN_BUFFER_LENGTH];
	void pc_add_line(const pc_group &g, const char *line) {
		mkdir("histories", 0775);

		char filename[256];
		sprintf(filename, "histories/%s", g.group);

		FILE *f = fopen(filename, "a+");
		if (!f)
			pc_fatal("pc_add_line: failed to open file '%s': %d.", filename, errno);

		fseek(f, 0, SEEK_END);
		size_t length = ftell(f);
		if (length >= sizeof(large_buffer))
			pc_fatal("pc_add_line: history file is too large.");

		pc_log("pc_add_line: length is %d", length);

		rewind(f);
		fread(large_buffer, 1, length, f);
		large_buffer[length] = 0;

		pc_log("pc_add_line: existing lines: %.128s", large_buffer);

		int lines = 0;
		char *without_first_line = NULL;
		for (char *p = large_buffer; *p; p++) {
			if (*p == '\n') {
				if (lines == 0)
					without_first_line = p + 1;
				lines++;
			}
		}

		char *write_back = large_buffer;
		if (lines + 1 > HIST_MAX)
			write_back = without_first_line;

		pc_log("pc_add_line: lines = '%d'.", lines);

		fclose(f);
		f = fopen(filename, "w");
		if (!f)
			pc_fatal("pc_add_line: failed to open file '%s': %d.", filename, errno);

		pc_log("pc_add_line: writing '%.128s' at %d..", write_back, ftell(f));
		fwrite(write_back, 1, strlen(write_back), f);
		pc_log("pc_add_line: strlen(write_back) = %d", strlen(write_back));

		sprintf(large_buffer, "%s\n", line);
		pc_log("pc_add_line: writing '%.128s' at %d..", large_buffer, ftell(f));
		fwrite(large_buffer, 1, strlen(large_buffer), f);
		pc_log("pc_add_line: strlen(large_buffer) = %d", strlen(large_buffer));

		size_t size = ftell(f);
		fclose(f);
	}

	void pc_send_lines(const pc_group &g, std::function<void(const char *)> sender) {
		char filename[256];
		sprintf(filename, "histories/%s", g.group);

		FILE *f = fopen(filename, "r");
		if (!f)
			return;

		char line_buffer[CONN_BUFFER_LENGTH];
		while (!feof(f)) {
			fread(line_buffer, 1, sizeof(line_buffer), f);
			sender(line_buffer);
		}

		fclose(f);
	}