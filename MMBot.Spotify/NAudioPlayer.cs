using System.Diagnostics;
using MMBot.Scripts;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MMBot.Spotify
{
    class NAudioPlayer : IPlayer
    {
        BufferedWaveProvider _buffer;

        private DirectSoundOut _currentOut;

        private bool _isMuted;
        private float _currentVolume = 1.0f;
        private VolumeWaveProvider16 _volumeWaveProvider;


        public int EnqueueSamples(int channels, int rate, byte[] samples, int frames)
        {
            if (_buffer == null)
            {
                _buffer = new BufferedWaveProvider(new WaveFormat(rate, channels));
                CreateOutput();
            }

            int space = _buffer.BufferLength - _buffer.BufferedBytes;
            if (space > samples.Length)
            {
                _buffer.AddSamples(samples, 0, samples.Length);
                return frames;
            }
            return 0;
        }

        private void CreateOutput()
        {
            var vwp = new VolumeWaveProvider16(_buffer);
            var dso = new DirectSoundOut(70);
            vwp.Volume = _currentVolume;
            Debug.WriteLine("Now playing at {0}% volume", dso.Volume);
            dso.Init(vwp);
            dso.Play();

            vwp.Volume = _currentVolume;

            _currentOut = dso;
            _volumeWaveProvider = vwp;
        }

        public void Reset()
        {
            if (_buffer != null)
                _buffer.ClearBuffer();
        }

        public void Mute()
        {
            if (_currentOut == null)
            {
                return;
            }
            _isMuted = true;
            _volumeWaveProvider.Volume = 0;
        }

        public void Unmute()
        {
            if (_currentOut == null)
            {
                return;
            }
            
            _isMuted = false;
            _volumeWaveProvider.Volume = _currentVolume;
        }

        public void TurnDown(int amount)
        {
            if (_currentOut == null)
            {
                return;
            }

            _currentVolume = System.Math.Max(0, _currentVolume - ((float)amount / 100));
            _volumeWaveProvider.Volume = _currentVolume;
        }

        public void TurnUp(int amount)
        {
            if (_currentOut == null)
            {
                return;
            }
            _currentVolume = System.Math.Min(1, _currentVolume + ((float)amount / 100));
            _volumeWaveProvider.Volume = _currentVolume;
        }
    }
}