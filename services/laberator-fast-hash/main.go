package hasher

/*
#cgo LDFLAGS: hasher.a -lm
#include <hasher.h>
 */
import "C"
import (
	"unsafe"
)

func GetHash(data []byte) [C.BLOCK_SIZE]byte {
	dataPtr := (*C.char)(unsafe.Pointer(&data[0]))
	var out [C.BLOCK_SIZE]byte
	outPtr := (*C.char)(unsafe.Pointer(&out[0]))
	dataLength := C.size_t(len(data))
	C.get_hash(dataPtr, dataLength, outPtr)
	return out
}
