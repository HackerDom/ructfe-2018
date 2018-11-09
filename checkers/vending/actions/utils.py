from random import shuffle
from string import ascii_letters


letters = list(ascii_letters + ascii_letters + ascii_letters + ascii_letters)


def get_rand_word(ln: int) -> str:
    ltrs = list(letters)
    shuffle(ltrs)
    return "".join(ltrs)[:ln]