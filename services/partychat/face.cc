#include "common.h"

int main() {

	pc_init_logging("/dev/null", true);

	addrinfo *endpoint;
	if (!pc_parse_endpoint("localhost:16770", &endpoint))
		pc_fatal("fuck!");

	pc_connection conn;
	if (!pc_connect(*endpoint, conn))
		pc_fatal("shit!");

	conn.send("Hey!");
	while (!conn.poll_send()) ;

	if (!conn.alive)
		pc_fatal("damn!");

	conn.send("Hoy!");
	while (!conn.poll_send()) ;

	if (!conn.alive)
		pc_fatal("damn!");

	conn.receive();

	while (!conn.poll_receive()) ;

	pc_log("Received: %s", conn.recv_buffer);

	return 0;
}