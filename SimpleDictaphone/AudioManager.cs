using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Windows;
using System.Threading.Tasks;

namespace SimpleDictaphone
{
    public class AudioManager
    {
        private Microphone _microphone;
        private MemoryStream _recordingStream;
        private byte[] _buffer;
        private string _currentFileName;

        public bool IsRecording { get; private set; }

        public AudioManager()
        {
            _microphone = Microphone.Default;
            _microphone.BufferDuration = TimeSpan.FromMilliseconds(500);
            _microphone.BufferReady += Microphone_BufferReady;

            
        }

        public string StartRecording()
        {
            

            if (IsRecording)
                return _currentFileName;

            _currentFileName = "recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
            _recordingStream = new MemoryStream();
            _buffer = new byte[_microphone.GetSampleSizeInBytes(_microphone.BufferDuration)];

            _microphone.Start();
            IsRecording = true;

            return _currentFileName;
        }

        public void StopRecording()
        {
            if (!IsRecording)
                return;

            _microphone.Stop();
            IsRecording = false;

            long len = _recordingStream != null ? _recordingStream.Length : 0;

            SaveWavToIsolatedStorage(_currentFileName);
        }



        private void Microphone_BufferReady(object sender, EventArgs e)
        {
            

            if (!IsRecording || _recordingStream == null)
                return;

            int count = _microphone.GetData(_buffer);

            if (count > 0)
            {
                _recordingStream.Write(_buffer, 0, count);
            }
        }



        private void SaveWavToIsolatedStorage(string fileName)
        {
            if (_recordingStream == null || _recordingStream.Length == 0)
            {
                MessageBox.Show(SimpleDictaphone.Resources.AppResources.EmptyBufferError, "(ᵕ⸝⸝• ᴗ •)", MessageBoxButton.OK);
                return;
            }

            var sampleRate = _microphone.SampleRate;
            int dataLength = (int)_recordingStream.Length;


            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (iso.FileExists(fileName))
                    iso.DeleteFile(fileName);

                var file = iso.CreateFile(fileName);

                try
                {
                    WriteWavHeaderRaw(file, dataLength, 1, sampleRate);

                    file.Flush();

                    _recordingStream.Position = 0;

                    _recordingStream.CopyTo(file);

                    file.Flush();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка в try: " + ex.Message, "(๑-﹏-๑)", MessageBoxButton.OK);
                }
                finally
                {
                    file.Dispose();
                }
            }

            _recordingStream.Dispose();
            _recordingStream = null;

            MessageBox.Show("Запись сохранена", "(˶˃ ᵕ ˂˶)", MessageBoxButton.OK);
        }

        private void WriteWavHeaderRaw(Stream stream, int dataLength, short channels, int sampleRate)
        {
            const short bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);
            int chunkSize = 36 + dataLength;
            
            byte[] header = new byte[44];

            header[0] = (byte)'R';
            header[1] = (byte)'I';
            header[2] = (byte)'F';
            header[3] = (byte)'F';

            BitConverter.GetBytes(chunkSize).CopyTo(header, 4);

            header[8] = (byte)'W';
            header[9] = (byte)'A';
            header[10] = (byte)'V';
            header[11] = (byte)'E';

            header[12] = (byte)'f';
            header[13] = (byte)'m';
            header[14] = (byte)'t';
            header[15] = (byte)' ';

            BitConverter.GetBytes(16).CopyTo(header, 16);

            BitConverter.GetBytes((short)1).CopyTo(header, 20);

            BitConverter.GetBytes(channels).CopyTo(header, 22);

            BitConverter.GetBytes(sampleRate).CopyTo(header, 24);

            BitConverter.GetBytes(byteRate).CopyTo(header, 28);

            BitConverter.GetBytes(blockAlign).CopyTo(header, 32);

            BitConverter.GetBytes(bitsPerSample).CopyTo(header, 34);

            header[36] = (byte)'d';
            header[37] = (byte)'a';
            header[38] = (byte)'t';
            header[39] = (byte)'a';

            BitConverter.GetBytes(dataLength).CopyTo(header, 40);

            stream.Write(header, 0, header.Length);
        }




        public string[] GetAllRecordings()
        {
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return iso.GetFileNames("recording_*.wav");
            }
        }
    }
}
