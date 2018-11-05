#pragma once

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <fcntl.h>
#include <errno.h>
#include <unistd.h>

// Logging

	void pc_init_logging(const char *file, bool echo);
	void pc_shutdown_logging();
	void pc_log(const char *format, ...);
	void pc_fatal(const char *format, ...);
	void pc_quit(const char *format, ...);

// Networking

	#define CONN_BUFFER_LENGTH 1024

	struct pc_connection {
		int socket = 0;

		char *recv_buffer;
		int recv_index = 0;
		int recv_length = 0;

		char *send_buffer;
		int send_index = 0;
		int send_length = 0;

		bool alive = false;

		pc_connection() = default;
		pc_connection(int sock);
		~pc_connection();
		pc_connection &operator=(pc_connection &&other);

		void send(const char *message, ...);
		int poll_send();
		bool is_sending();

		void receive();
		int poll_receive();
		bool is_receiving();
	};

	bool pc_connect(const addrinfo &endpoint, pc_connection &connection);
	void pc_make_nonblocking(int socket);

	bool pc_parse_endpoint(const char *str, addrinfo **endpoint);

	int pc_start_server(int port);
	int pc_accept_client(int server_sock);