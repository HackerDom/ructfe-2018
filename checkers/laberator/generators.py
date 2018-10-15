from random import randint, choice
from string import ascii_letters, digits


with open("names") as names_file:
    NAMES = names_file.read().split('\n')

with open("user-agents") as user_agents_file:
    USER_AGENTS = user_agents_file.read().split('\n')


with open("fonts") as file:
    FONTS = file.read().split('\n')


with open("food") as file:
    LABELS = file.read().split('\n')


def generate_login():
    return choice(NAMES) + "_" + "".join(choice(digits) for _ in range(20))


def generate_password():
    return "".join(choice(ascii_letters + digits) for _ in range(20))


def generate_user_agent():
    return choice(USER_AGENTS)


def generate_headers():
    return {'User-Agent': generate_user_agent()}


def generate_label():
    return choice(FONTS), randint(10, 30)
