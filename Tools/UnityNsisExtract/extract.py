"""Read-only NSISBI payload extraction; never executes installer commands.

Adapted from https://github.com/kmod-midori/unity-nsisbi-ext
Copyright (c) 2024 Midori Kochiya, MIT; see LICENSE-upstream.
Python port adds path/bounds checks. Only non-solid LZMA is supported.
"""
import argparse
import json
import lzma
import mmap
import struct
from pathlib import Path


def u32(data, at):
    return struct.unpack_from('<I', data, at)[0]


def decode(data, expected=None):
    if len(data) < 5:
        raise ValueError('Truncated LZMA properties')
    prop = data[0]
    lc, remainder = prop % 9, prop // 9
    lp, pb = remainder % 5, remainder // 5
    filters = [dict(id=lzma.FILTER_LZMA1, dict_size=u32(data, 1), lc=lc, lp=lp, pb=pb)]
    decoder = lzma.LZMADecompressor(format=lzma.FORMAT_RAW, filters=filters)
    result = decoder.decompress(data[5:])
    if expected is not None and len(result) != expected:
        raise ValueError(f'Header length {len(result)} != {expected}')
    if not decoder.eof and expected is None:
        raise ValueError('Incomplete compressed file')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('installer', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--list', action='store_true')
    args = parser.parse_args()
    root = args.output.resolve()
    with args.installer.open('rb') as handle, mmap.mmap(handle.fileno(), 0, access=mmap.ACCESS_READ) as data:
        pos = data.find(b'\xef\xbe\xad\xdeNullsoftInst') - 4
        if pos < 0 or pos % 512:
            raise ValueError('NSIS header not found at aligned offset')
        flags, _, _, _, _, header_size, archive_size = struct.unpack_from('<7I', data, pos)
        if not flags & 0x30:
            raise ValueError('Only NSISBI supported')
        cursor = pos + 36
        compressed_size = u32(data, cursor)
        cursor += 4
        if not compressed_size & 0x80000000:
            raise ValueError('Expected non-solid compressed header')
        compressed_size &= 0x7fffffff
        header = decode(data[cursor:cursor + compressed_size], header_size)
        data_start = cursor + compressed_size
        blocks = [struct.unpack_from('<2I', header, 4 + i * 8) for i in range(8)]
        strings = header[blocks[3][0]:blocks[4][0]]
        if strings[:2] != b'\0\0':
            raise ValueError('Expected Unicode strings')

        def string(index):
            chars = []
            offset = index * 2
            while True:
                ch = struct.unpack_from('<H', strings, offset)[0]
                offset += 2
                if ch == 0:
                    return ''.join(chars).replace('\\', '/').lstrip('/')
                if ch in (2, 3):
                    offset += 2  # Strip installer variable tokens, not execute them.
                else:
                    chars.append(chr(ch))

        current = ''
        manifest = []
        for i in range(blocks[2][1]):
            command, *params = struct.unpack_from('<9I', header, blocks[2][0] + i * 36)
            if command == 11:
                current = string(params[0])
            elif command == 20:
                relative = Path(current) / string(params[1])
                destination = (root / relative).resolve()
                if not destination.is_relative_to(root) or ':' in str(relative):
                    raise ValueError(f'Unsafe output path {relative}')
                at = data_start + params[2]
                size_flag = u32(data, at)
                size = size_flag & 0x7fffffff
                if at + 4 + size > len(data):
                    raise ValueError('Payload beyond archive')
                manifest.append(dict(path=relative.as_posix(), packed_size=size, compressed=bool(size_flag & 0x80000000)))
                if not args.list:
                    payload = data[at + 4:at + 4 + size]
                    if size and size_flag & 0x80000000:
                        payload = decode(payload)
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    destination.write_bytes(payload)
        root.mkdir(parents=True, exist_ok=True)
        (root / 'extraction-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
        print(f'NSISBI flags={flags}, header={header_size}, archive={archive_size}, files={len(manifest)}, list_only={args.list}')


if __name__ == '__main__':
    main()
