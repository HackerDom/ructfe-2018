using System;
using System.Collections;
using System.Collections.Generic;

namespace Transmitter.Morse
{
	public class SignalToPcmConverter : IEnumerator<double>
	{
		private readonly IEnumerator<Signals> signals;
		private readonly double dotDuration;
		private readonly double dphi;
		private readonly double dt;

		private double phi = 0;
		private double timeDot = 0;

		public SignalToPcmConverter(IEnumerator<Signals> signals, double dotDuration, double freq, double dt)
		{
			this.signals = signals;
			this.dotDuration = dotDuration;
			dphi = dt * freq;
			this.dt = dt;
		}

		public bool MoveNext()
		{
			if (timeDot > dotDuration)
			{
				if (!signals.MoveNext())
					return false;
				timeDot = 0;
			}

			phi += dphi;
			timeDot += dt;
			switch (signals.Current)
			{
				case Signals.Silence:
					Current = 0;
					break;
				case Signals.Signal:
					Current = Math.Sin(phi);
					break;
				default:
					Current = NoiseGenerator.Get();
					break;
			}

			return true;
		}

		public void Reset()
			=> signals.Reset();

		public double Current { get; private set; }

		object IEnumerator.Current
			=> Current;

		public void Dispose()
			=> signals.Dispose();
	}
}