#include <stdlib.h>
#include <time.h>
#include <stdarg.h>

#include "common.h"
#include "commands.h"
#include "words"

// TODO
// 1. retry with different names
// 2. implement 'ready' command
// 3. handle 'end' and 'die'

#define OK 101
#define CORRUPT 102
#define MUMBLE 103
#define DOWN 104
#define CHECKER_ERROR 110

void make_name(char *buffer) {

	const char *pt1 = adjs[rand() % 600];
	const char *pt2 = nouns[rand() % 1100];

	sprintf(buffer, "@%s%s%d", pt1, pt2, rand() % 1000);
}

bool get_team_name(char *host, char *buffer) {
	char buf[256];
	strcpy(buf, host);

	strtok(host, ".");
	char *part = strtok(NULL, ".");

	if (!part)
		return false;

	strcpy(buffer, part);
	return true;
}

addrinfo *get_master_addr() {
	addrinfo *addr = NULL;
	pc_parse_endpoint("10.10.10.100:16770", &addr);
	return addr;
}

void checker_fail(int status, const char *format, ...) {
	va_list args;

	va_start(args, format);

	vfprintf(stderr, format, args);

	va_end(args);

	exit(status);
}

void checker_pass() {
	exit(OK);
}

void handle_check(int argc, char ** argv) {
	if (argc < 3)
		checker_fail(CHECKER_ERROR, "check: not enough arguments\n");

	char *host = argv[2];
	char team_name[64];
	if (!get_team_name(host, team_name))
		checker_fail(CHECKER_ERROR, "check: failed to extract team name from host '%s'\n", host);
	char team_nick[64];
	sprintf(team_nick, "@%s", team_name);

	char checker_name[64];
	make_name(checker_name);

	checker_state state(team_nick);
	connection<checker_state> conn(*get_master_addr(), state);
	conn.flush(conn.send<hb_command<checker_state>>(checker_name));
	conn.flush(conn.send<list_command<checker_state>>(""));

	if (state.team_listed)
		checker_pass();
	else
		checker_fail(DOWN, "check: team '%s' was not listed\n", team_nick);
}

void handle_put(int argc, char **argv) {
	if (argc < 5)
		checker_fail(CHECKER_ERROR, "put: not enough arguments\n");

	char *host = argv[2];
	char *flag_id = argv[3];
	char *flag = argv[4];

	if (strlen(flag) != 32)
		checker_fail(CHECKER_ERROR, "Flag must be of length 32\n");

	char team_name[64];
	if (!get_team_name(host, team_name))
		checker_fail(CHECKER_ERROR, "put: failed to extract team name from host '%s'\n", host);
	char team_nick[64];
	sprintf(team_nick, "@%s", team_name);

	char checker_name[64];
	make_name(checker_name);

	char text[64];
	sprintf(text, "%s %s", flag, team_nick);

	checker_state state(team_nick);
	connection<checker_state> conn(*get_master_addr(), state);
	conn.flush(conn.send<hb_command<checker_state>>(checker_name));
	conn.flush(conn.send<say_command<checker_state>>(text));

	printf("%s\n", checker_name);

	checker_pass();
}

void handle_get(int argc, char **argv) {
	if (argc < 5)
		checker_fail(CHECKER_ERROR, "get: not enough arguments\n");

	char *host = argv[2];
	char *flag_id = argv[3];
	char *flag = argv[4];

	if (flag_id[0] != '@')
		checker_fail(CHECKER_ERROR, "Flag ID must start with @\n");
	if (strlen(flag) != 32)
		checker_fail(CHECKER_ERROR, "Flag must be of length 32\n");

	char team_name[64];
	if (!get_team_name(host, team_name))
		checker_fail(CHECKER_ERROR, "get: failed to extract team name from host '%s'\n", host);
	char team_nick[64];
	sprintf(team_nick, "@%s", team_name);

	checker_state state(team_nick, flag);
	connection<checker_state> conn(*get_master_addr(), state);
	conn.flush(conn.send<hb_command<checker_state>>(flag_id));
	conn.flush(conn.send<history_command<checker_state>>(team_nick));

	if (state.flag_found)
		checker_pass();
	else
		checker_fail(CORRUPT, "get: flag '%s' was not found in history\n", flag);
}

int main(int argc, char **argv) {
	srand(time(NULL));

	if (argc < 2)
		checker_fail(CHECKER_ERROR, 
			"Usage: \n"
			"%s check <team>\n"
			"%s put <team> <flag-id> <flag>\n"
			"%s get <team> <flag-id> <flag>\n",
			argv[0], argv[0], argv[0]);

	//pc_init_logging("checker.log", true);

	if (!strcmp("check", argv[1]))
		handle_check(argc, argv);

	if (!strcmp("put", argv[1]))
		handle_put(argc, argv);

	if (!strcmp("get", argv[1]))
		handle_get(argc, argv);

	checker_fail(CHECKER_ERROR, "Unrecognized command '%s'\n", argv[1]);
}