%.o: %.cc
	g++ -c -o $@ $<

all: face node checker

FACE_OBJS = face.o
face: $(FACE_OBJS)
	g++ -o pc-face $(FACE_OBJS)

NODE_OBJS = node.o
node: $(NODE_OBJS)
	g++ -o pc-node $(NODE_OBJS)

CHECKER_OBJS = checker.o
checker: $(CHECKER_OBJS)
	g++ -o checker $(CHECKER_OBJS)

clean:
	rm -f pc-* checker *.o