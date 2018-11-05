all:
	rm -rf lib
	gcc -Wall -c hasher.c -o hasher.o
	ar -cq hasher.a hasher.o
	rm hasher.o
	mkdir lib
	mv hasher.a lib

