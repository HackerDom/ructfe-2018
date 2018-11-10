from scipy import signal
import numpy
import itertools

def compress(arr):
	res = []
	curr = False
	st = 0
	for i, v in numpy.ndenumerate(arr):
		if v != curr:
			res.append(i[0] - st)
			st = i[0]
			curr = v
	res.append(arr.size - st)
	first = 1 if res[0] else 2
	return first, numpy.array(res[first:-1])

def get_bounds(arr):
	res = []
	for i,x in numpy.ndenumerate(arr):
		i = i[0]
		if i == arr.size - 1:
			continue
		if 2 * x <= arr[i + 1]:
			res.append(x)
	return res

def apply_silence_bounds(arr, bounds):
	res = []
	for x in numpy.nditer(arr):
		if x <= bounds[0]:
			res.append('')
		elif len(bounds) > 1 and bounds[1] < x:
			res.append(' | ')
		else:
			res.append(' ')
	return res

def merge(signal, silence):
	morse = ''
	for f,t in itertools.zip_longest(signal, silence):
		if f is not None:
			morse += f
		if t is not None:
			morse += t
	return morse

def convert_to_text(morse):
	chars = morse.split()
	if morse[0] != ' ':
		chars = chars[1:]
	if morse[-1] != ' ':
		chars = chars[:-1]
	res = ''
	for ch in chars:
		if ch not in morseMapping:
			checker.mumble(message='Unknown sequence {}'.format(ch))
		res += morseMapping[ch]
	return res

class MorseParser:
	def __init__(self, freq):
		self.freq = freq
		self.specs = []

	def save(self, raw):
		self.specs.append(self.prepare(raw))

	def prepare(self, raw):
		data = numpy.frombuffer(raw, dtype=numpy.uint8)
		data = numpy.float64(data) / 127.0 - 1
		freq, _, Sxx = signal.spectrogram(data, 8000)

		nearest = numpy.abs(freq - self.freq).argmin()
		res = Sxx[nearest]
		if nearest != 0:
			res = res + Sxx[nearest - 1]
		if nearest != numpy.size(Sxx, 0) - 1:
			res = res + Sxx[nearest + 1]
		return res

	def process(self):
		if len(self.specs) == 0:
			checker.down(message='No one signal package')

		data = numpy.concatenate(self.specs)
		mx = numpy.max(data)
		data = numpy.log(data / mx) >= -1.5
		skiped, comp = compress(data)

		if len(comp) < 2:
			checker.corrupt(message='No signal')

		bounds = get_bounds(numpy.unique(comp))
		silence = comp[1::2]
		signal = comp[0::2]
		if skiped == 2:
			signal, silence = silence, signal
		signal = list(numpy.where(signal <= bounds[0], '.', '-'))
		silence = apply_silence_bounds(silence, bounds)
		if skiped == 2:
			signal, silence = silence, signal
		morse = merge(signal, silence)
		text = convert_to_text(morse)

		if not text:
			checker.corrupt(message='No one symbol in signal')

		return text


morseMapping = {
	'.-': 'A',
	'-...': 'B',
	'-.-.': 'C',
	'-..': 'D',
	'.': 'E',
	'..-.': 'F',
	'--.': 'G',
	'....': 'H',
	'..': 'I',
	'.---': 'J',
	'-.-': 'K',
	'.-..': 'L',
	'--': 'M',
	'-.': 'N',
	'---': 'O',
	'.--.': 'P',
	'--.-': 'Q',
	'.-.': 'R',
	'...': 'S',
	'-': 'T',
	'..-': 'U',
	'...-': 'V',
	'.--': 'W',
	'-..-': 'X',
	'-.--': 'Y',
	'--..': 'Z',
	'-----': '0',
	'.----': '1',
	'..---': '2',
	'...--': '3',
	'....-': '4',
	'.....': '5',
	'-....': '6',
	'--...': '7',
	'---..': '8',
	'----.': '9',
	'.-...': '&',
	'.----.': "'",
	'.--.-.': '@',
	'-.--.-': ')',
	'-.--.': '(',
	'---...': ':',
	'--..--': ',',
	'-...-': '=',
	'-.-.--': '!',
	'.-.-.-': '.',
	'-....-': '-',
	'.-.-.': '+',
	'.-..-.': '"',
	'..--..': '?',
	'-..-.': '/',
	'|': ' '
}
