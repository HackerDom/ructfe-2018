#include "common.h"
#include "commands.h"

const char banner[] =
	" ▄▄▄· ▄▄▄· ▄▄▄  ▄▄▄▄▄ ▄· ▄▌ ▄▄·  ▄ .▄ ▄▄▄· ▄▄▄▄▄\n"\
	"▐█ ▄█▐█ ▀█ ▀▄ █·•██  ▐█▪██▌▐█ ▌▪██▪▐█▐█ ▀█ •██  \n"\
	" ██▀·▄█▀▀█ ▐▀▀▄  ▐█.▪▐█▌▐█▪██ ▄▄██▀▐█▄█▀▀█  ▐█.▪\n"\
	"▐█▪·•▐█ ▪▐▌▐█•█▌ ▐█▌· ▐█▀·.▐███▌██▌▐▀▐█ ▪▐▌ ▐█▌·\n"\
	".▀    ▀  ▀ .▀  ▀ ▀▀▀   ▀ • ·▀▀▀ ▀▀▀ · ▀  ▀  ▀▀▀\n";

char current_group[256];

bool handle_command(const char *command, const char *args, connection &conn) {
	if (!strcmp("!help", command)) {
		printf("Available commands\n:");
		printf("\t!help - display this message.\n");
		printf("\t!quit - exit partychat.\n");

		return true;
	}
	if (!strcmp("!quit", command)) {
		return false;
	}
	if (!strcmp("!say", command)) {
		// load history if needed
		conn.send<say_command>(args);
		conn.tick();conn.tick();conn.tick();
		return true;
	}

	printf("Unrecognized command '%s'.\n", command);
	return true;
}

int main(int argc, char **argv) {

	char endpoint[256];
	if (argc == 1)
		sprintf(endpoint, "%s", "localhost:6666");
	else if (argc == 2)
		sprintf(endpoint, "%s", argv[1]);
	else {
		printf("Usage:\n");
		printf("%s [node-endpoint]\n", argv[0]);
		exit(-1);
	}

	printf("%s\n", banner);

	char log_file[64];
	sprintf(log_file, "%s.log", argv[0]);
	pc_init_logging(log_file, true);

	addrinfo *addr;
	if (!pc_parse_endpoint(endpoint, &addr))
		pc_fatal("Failed to parse node endpoint.");

	pc_log("Establishing connection to node at %s..", endpoint);

	connection conn(*addr);

	printf("Welcome! Type !help if not sure.\n");

	char last_cmd[512];
	bzero(last_cmd, sizeof(last_cmd));
	while (true) {
		printf("%s> ", last_cmd);

		char buffer[512];
		bzero(buffer, sizeof(buffer));
		if (!fgets(buffer, sizeof(buffer), stdin))
			break;
		if (strlen(buffer) > 0)
			buffer[strlen(buffer) - 1] = 0;

		char *args = buffer;
		if (buffer[0] == '!') {
			if (strchr(buffer, ' ')) {
				char *tok = strtok(buffer, " ");
				strcpy(last_cmd, tok);
				args = buffer + strlen(last_cmd) + 1;
			}
			else {
				strcpy(last_cmd, buffer);
				args = NULL;
			}
		}
		else if (!last_cmd[0])
			continue;

		pc_log("Executing command '%s' with args '%s'..", last_cmd, args);

		if (!handle_command(last_cmd, args, conn))
			break;
	}

	pc_shutdown_logging();

	return 0;
}