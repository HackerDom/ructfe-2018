from random import choice
from string import ascii_uppercase, digits
from subprocess import check_output, CalledProcessError
from time import sleep

ALPH = ascii_uppercase + digits
MAX_REQUESTS = 10 ** 6
HOST = "localhost"
OK, CORRUPT, MUMBLE, DOWN, CHECKER_ERROR = 101, 102, 103, 104, 110
CODES = {
    OK: "OK",
    CORRUPT: "CORRUPT",
    MUMBLE: "MUMBLE",
    DOWN: "DOWN",
    CHECKER_ERROR: "CHECKER_ERROR"
}


def generate_flag():
    return ''.join(choice(ALPH) for _ in range(31)) + "="


def main():
    for i in range(MAX_REQUESTS):
        sleep(3)
        flag = generate_flag()
        vuln = str(i % 2 + 1)
        try:
            check_output(["python3", "./checker.py", "put", HOST, "dummy flag id", flag, vuln])
        except CalledProcessError as exc:
            print("REQ_ID: {}, CMD: PUT, FLAG: {}, VULN: {}, STATUS: {}".format(i, flag, vuln, CODES[exc.returncode]))
            if exc.returncode != OK:
                continue
            flag_id = exc.output.decode().strip('\n')
            try:
                check_output(["python3", "./checker.py", "get", HOST, flag_id, flag, vuln])
            except CalledProcessError as get_exc:
                print("REQ_ID: {}, CMD: GET, FLAG_ID: {}, FLAG: {}, VULN: {}, STATUS: {}".format(
                    i, flag_id, flag, vuln, CODES[get_exc.returncode])
                )


if __name__ == '__main__':
    main()
