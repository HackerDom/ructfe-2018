CFLAGS = -std=c++11

%.o: %.cc
	g++ -c -o $@ $< $(CFLAGS)

all: clean face node checker

FACE_OBJS = face.o common.o
face: $(FACE_OBJS)
	g++ -o partychat $(FACE_OBJS)

NODE_OBJS = node.o common.o
node: $(NODE_OBJS)
	g++ -o partychat-node $(NODE_OBJS)

CHECKER_OBJS = checker.o common.o
checker: $(CHECKER_OBJS)
	g++ -o checker $(CHECKER_OBJS)
	cp checker ../../checkers/partychat/

clean:
	rm -rf partychat* checker *.o histories