#!/usr/bin/env python3
"""Generate a minimal valid MP3 fixture (ID3v2.3 tags + silent MPEG1 Layer III frames).

Used by the smoke test so no binary media has to be checked into the repository
and no external encoder (ffmpeg/lame) is needed to run it.
"""
import struct
import sys


def id3_frame(fid, text):
    payload = b"\x00" + text.encode("latin-1")
    return fid.encode() + struct.pack(">I", len(payload)) + b"\x00\x00" + payload


def make_mp3(path, title, artist="Test Artist", album="Test Album", genre="Rock", seconds=5):
    frames = (
        id3_frame("TIT2", title)
        + id3_frame("TPE1", artist)
        + id3_frame("TALB", album)
        + id3_frame("TCON", genre)
    )
    size = len(frames)
    syncsafe = bytes([(size >> 21) & 0x7F, (size >> 14) & 0x7F, (size >> 7) & 0x7F, size & 0x7F])
    id3 = b"ID3\x03\x00\x00" + syncsafe + frames

    # MPEG1 Layer III, 128 kbps, 44100 Hz, stereo, no padding -> 417-byte frames, ~38.28 frames/sec
    frame = b"\xff\xfb\x90\x00" + b"\x00" * 413
    frame_count = int(seconds * 44100 / 1152) + 1

    with open(path, "wb") as f:
        f.write(id3)
        for _ in range(frame_count):
            f.write(frame)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "test_song.mp3"
    make_mp3(out, "Test Song")
    print("wrote " + out)
