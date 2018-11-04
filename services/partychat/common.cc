#include <stdarg.h>
#include <time.h>

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
		vsprintf(message, format, args);
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
		vsprintf(message, format, args);
		va_end(args);

		pc_shutdown_logging();

		printf("Fatal: %s\n", message);
		exit(-1);
	}

// Networking

	#define CONN_BUFFER_LENGTH 1024

	pc_connection::pc_connection(int sock) {
		socket = sock;
		recv_buffer = new char[CONN_BUFFER_LENGTH];
		send_buffer = new char[CONN_BUFFER_LENGTH];
		alive = true;
	}

	pc_connection::~pc_connection() {
		if (socket) {
			close(socket);
			delete[] recv_buffer;
			delete[] send_buffer;
		}
	}
	
	pc_connection &pc_connection::operator=(pc_connection &&other) {
		memcpy(this, &other, sizeof(pc_connection));
		other.socket = 0;
		return *this;
	}

	void pc_connection::send(const char *message) {
		if (!alive)
			return;

		if (send_length != 0)
			pc_fatal("pc_connection::send: previous send operation has not completed.");

		int message_length = strlen(message) + 1;

		if (message_length >= CONN_BUFFER_LENGTH)
			pc_fatal("pc_connection::send: message was too long.");

		strcpy(send_buffer, message);
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

		recv_buffer[recv_index] = 0;
		char *endl = strchr(recv_buffer, '\n');
		if (endl) {
			*endl = 0;
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