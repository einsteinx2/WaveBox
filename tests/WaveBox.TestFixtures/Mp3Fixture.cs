using System;
using System.IO;
using System.Text;

namespace WaveBox.TestFixtures {
    /// <summary>
    /// Generates a minimal valid MP3 (ID3v2.3 tags + silent MPEG1 Layer III frames) so no binary
    /// media has to be checked into the repository and no external encoder is needed.
    /// C# port of the retired tests/fixtures/make_fixture.py.
    /// </summary>
    public static class Mp3Fixture {
        public static void Write(string path, string title = "Test Song", string artist = "Test Artist", string album = "Test Album", string genre = "Rock", int seconds = 5) {
            byte[] frames;
            using (MemoryStream tagStream = new MemoryStream()) {
                WriteId3Frame(tagStream, "TIT2", title);
                WriteId3Frame(tagStream, "TPE1", artist);
                WriteId3Frame(tagStream, "TALB", album);
                WriteId3Frame(tagStream, "TCON", genre);
                frames = tagStream.ToArray();
            }

            int size = frames.Length;
            byte[] syncsafe = new byte[] {
                (byte)((size >> 21) & 0x7F), (byte)((size >> 14) & 0x7F), (byte)((size >> 7) & 0x7F), (byte)(size & 0x7F)
            };

            // MPEG1 Layer III, 128 kbps, 44100 Hz, stereo, no padding -> 417-byte frames, ~38.28 frames/sec
            byte[] mpegFrame = new byte[417];
            mpegFrame[0] = 0xFF;
            mpegFrame[1] = 0xFB;
            mpegFrame[2] = 0x90;
            mpegFrame[3] = 0x00;
            int frameCount = (seconds * 44100 / 1152) + 1;

            using (FileStream f = File.Create(path)) {
                f.Write(Encoding.ASCII.GetBytes("ID3"), 0, 3);
                f.Write(new byte[] { 0x03, 0x00, 0x00 }, 0, 3);
                f.Write(syncsafe, 0, 4);
                f.Write(frames, 0, frames.Length);
                for (int i = 0; i < frameCount; i++) {
                    f.Write(mpegFrame, 0, mpegFrame.Length);
                }
            }
        }

        private static void WriteId3Frame(MemoryStream stream, string frameId, string text) {
            byte[] payload = new byte[text.Length + 1];
            payload[0] = 0x00; // ISO-8859-1 text encoding marker
            Encoding.Latin1.GetBytes(text, 0, text.Length, payload, 1);

            stream.Write(Encoding.ASCII.GetBytes(frameId), 0, 4);
            int length = payload.Length;
            stream.Write(new byte[] {
                (byte)((length >> 24) & 0xFF), (byte)((length >> 16) & 0xFF), (byte)((length >> 8) & 0xFF), (byte)(length & 0xFF)
            }, 0, 4);
            stream.Write(new byte[] { 0x00, 0x00 }, 0, 2); // flags
            stream.Write(payload, 0, payload.Length);
        }
    }
}
