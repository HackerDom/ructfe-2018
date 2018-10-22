import argparse
import sys
from random import choice
from string import ascii_uppercase, digits
from subprocess import check_output, CalledProcessError
from time import sleep

ALPH = ascii_uppercase + digits

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
    while True:
        sleep(1)
        flag = generate_flag()
        try:
            check_output(["python3", "./checker.py", "put", "localhost", "dummy flag id", flag, "1"])
        except CalledProcessError as exc:
            print("REQ_ID: {}, CMD: PUT, FLAG: {}, STATUS: {}".format(i, flag, CODES[exc.returncode]))
            if exc.returncode != OK:
                continue
            flag_id = exc.output.decode().strip('\n')
            try:
                check_output(["python3", "./checker.py", "get", "localhost", flag_id, flag, "1"])
            except CalledProcessError as get_exc:
                print("REQ_ID: {}, CMD: GET, FLAG_ID: {}, FLAG: {}, STATUS: {}".format(
                    i, flag_id, flag, CODES[get_exc.returncode])
                )


if __name__ == '__main__':
    main()
