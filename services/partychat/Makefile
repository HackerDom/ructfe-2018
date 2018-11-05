CFLAGS = -std=c++11

%.o: %.cc
	g++ -c -o $@ $< $(CFLAGS)

all: face node checker

FACE_OBJS = face.o commands.o common.o
face: $(FACE_OBJS)
	g++ -o partychat $(FACE_OBJS)

NODE_OBJS = node.o commands.o common.o
node: $(NODE_OBJS)
	g++ -o partychat-node $(NODE_OBJS)

CHECKER_OBJS = checker.o common.o
checker: $(CHECKER_OBJS)
	g++ -o checker $(CHECKER_OBJS)

clean:
	rm -f partychat* checker *.o