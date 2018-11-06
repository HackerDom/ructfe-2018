package hasher

/*
#cgo LDFLAGS: ${SRCDIR}/lib/hasher.a -lm
#include <hasher.h>
 */
import "C"
import (
	"unsafe"
)

func GetHash(data []byte) [C.BLOCK_SIZE]byte {
	var out [C.BLOCK_SIZE]byte
	if len(data) == 0 {
		for i := 0; i < C.BLOCK_SIZE; i++ {
			out[i] = byte(i * i * i ^ i * i + i)
		}
		return out
	}
	dataPtr := (*C.char)(unsafe.Pointer(&data[0]))
	outPtr := (*C.char)(unsafe.Pointer(&out[0]))
	dataLength := C.size_t(len(data))
	C.get_hash(dataPtr, dataLength, outPtr)
	return out
}
