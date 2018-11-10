#include <poll.h>

#include "common.h"
#include "commands.h"

const char banner[] =
	" ▄▄▄· ▄▄▄· ▄▄▄  ▄▄▄▄▄ ▄· ▄▌ ▄▄·  ▄ .▄ ▄▄▄· ▄▄▄▄▄\n"\
	"▐█ ▄█▐█ ▀█ ▀▄ █·•██  ▐█▪██▌▐█ ▌▪██▪▐█▐█ ▀█ •██  \n"\
	" ██▀·▄█▀▀█ ▐▀▀▄  ▐█.▪▐█▌▐█▪██ ▄▄██▀▐█▄█▀▀█  ▐█.▪\n"\
	"▐█▪·•▐█ ▪▐▌▐█•█▌ ▐█▌· ▐█▀·.▐███▌██▌▐▀▐█ ▪▐▌ ▐█▌·\n"\
	".▀    ▀  ▀ .▀  ▀ ▀▀▀   ▀ • ·▀▀▀ ▀▀▀ · ▀  ▀  ▀▀▀\n";

bool handle_command(const char *command, const char *args, connection<face_state> &conn) {
	if (!strcmp("!help", command)) {
		printf("Available commands:\n");
		printf("\t!help - display this message.\n");
		printf("\t!list - see who's online.\n");
		printf("\t!say <message> - say something.\n");
		printf("\t!history <group> - show the history of a conversation.\n");
		printf("\t!quit - exit partychat.\n");

		return true;
	}
	if (!strcmp("!quit", command)) {
		conn.flush(conn.send<end_command<face_state>>(args));
		return false;
	}
	if (!strcmp("!list", command)) {
		conn.flush(conn.send<list_command<face_state>>(args));
		return true;
	}
	if (!strcmp("!say", command)) {
		conn.flush(conn.send<say_command<face_state>>(args));
		return true;
	}
	if (!strcmp("!history", command)) {
		conn.flush(conn.send<history_command<face_state>>(args));
		return true;
	}

	printf("Unrecognized command '%s'.\n", command);
	return true;
}

int main(int argc, char **argv) {

	printf("%s\n", banner);

	char log_file[64];
	sprintf(log_file, "%s.log", argv[0]);
	pc_init_logging(log_file, false);

	addrinfo *addr;
	if (!pc_parse_endpoint("127.0.0.1:6666", &addr))
		pc_fatal("Failed to parse node endpoint.");

	pc_log("Connecting to node: %s:%d", 
                inet_ntoa(((sockaddr_in *)addr->ai_addr)->sin_addr), 
                ntohs(((sockaddr_in *)addr->ai_addr)->sin_port));

	face_state state;
	connection<face_state> conn(*addr, state);

	printf("Welcome! Type !help if not sure.\n");

	pollfd fds[2];
	memset(&fds, 0, sizeof(fds));

	fds[0].fd = conn.conn.socket;
	fds[0].events = POLLIN;
	fds[1].fd = 0;
	fds[1].events = POLLIN;

	char last_cmd[512];
	bzero(last_cmd, sizeof(last_cmd));
	while (true) {
		
		printf("%s> ", last_cmd);
		fflush(stdout);

		int result = poll(fds, 2, -1);
		if (result < 0)
			pc_fatal("main: poll() failed unexpectedly.");

		if (fds[1].revents == POLLIN) {

			char buffer[512];
			bzero(buffer, sizeof(buffer));
			if (!fgets(buffer, sizeof(buffer), stdin))
				break;
			if (buffer[0] && buffer[0] != '\n')
				buffer[strlen(buffer) - 1] = 0;
			else
				strcpy(buffer, last_cmd);

			if (buffer[0] != '!')
				continue;

			strcpy(last_cmd, buffer);

			char cmd[512];
			char *args = buffer;
			if (strchr(buffer, ' ')) {
				char *tok = strtok(buffer, " ");
				strcpy(cmd, tok);
				args = buffer + strlen(cmd) + 1;
			}
			else {
				strcpy(cmd, buffer);
				args = NULL;
			}

			pc_log("Executing command '%s' with args '%s'..", cmd, args);

			if (!handle_command(cmd, args, conn))
				break;
		}
		else {
			printf("\r");
			conn.tick();
		}
	}

	pc_shutdown_logging();

	return 0;
}
