#include <stdlib.h>
#include <time.h>
#include <stdarg.h>

#include "common.h"
#include "words"

#define OK 101
#define CORRUPT 102
#define MUMBLE 103
#define DOWN 104
#define CHECKER_ERROR 110

void make_name(char *buffer) {

	const char *pt1 = adjs[rand() % 600];
	const char *pt2 = nouns[rand() % 1100];

	sprintf(buffer, "%s%s", pt1, pt2);
}

bool get_team_name(char *ip, char *buffer) {
	char *part = strtok(ip)
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

	char *team = argv[2];

	checker_pass();
}

void handle_put(int argc, char **argv) {
	if (argc < 5)
		checker_fail(CHECKER_ERROR, "put: not enough arguments\n");

	char *team = argv[2];
	char *flag_id = argv[3];
	char *flag = argv[4];

	if (strlen(flag) != 32)
		checker_fail(CHECKER_ERROR, "Flag must be of length 32\n");

	char name[64];
	make_name(name);

	printf("%s\n", name);

	checker_pass();
}

void handle_get(int argc, char **argv) {
	if (argc < 5)
		checker_fail(CHECKER_ERROR, "get: not enough arguments\n");

	char *team = argv[2];
	char *flag_id = argv[3];
	char *flag = argv[4];

	if (flag_id[0] != '@')
		checker_fail(CHECKER_ERROR, "Flag ID must start with @\n");
	if (strlen(flag) != 32)
		checker_fail(CHECKER_ERROR, "Flag must be of length 32\n");

	checker_pass();
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

	if (!strcmp("check", argv[1]))
		handle_check(argc, argv);

	if (!strcmp("put", argv[1]))
		handle_put(argc, argv);

	if (!strcmp("get", argv[1]))
		handle_get(argc, argv);

	checker_fail(CHECKER_ERROR, "Unrecognized command '%s'\n", argv[1]);
}