from scipy import signal
import numpy


class SoundFinder:
	def __init__(self):
		self.result = False
	def get_new_data(self, raw):
		if self.result:
			return
		data = numpy.frombuffer(raw, dtype=numpy.uint8)
		data = numpy.float64(data) / 127.0 - 1
		_, _, Sxx = signal.spectrogram(data, 8000)
		mx = numpy.amax(Sxx)
		mn = numpy.amin(Sxx)
		if mx == 0 and mn == 0:
			return
		if numpy.log(abs(mx - mn) / mx) >= -1.5:
			self.result = True
