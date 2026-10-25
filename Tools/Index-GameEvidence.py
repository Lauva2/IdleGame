"""Index actual local PNG/MP4 evidence and losslessly store sampled video frames.

Usage: python Tools/Index-GameEvidence.py <QA-folder> <absolute-frame-root>
       [--index-only]

Source PNGs/videos are never rewritten, moved or uploaded. Video basenames
starting shop-closeup, initial-flow or fishing-motion use 10FPS; other videos use 2FPS. Sampling
PTS is approximate output time, not an exact source frame or touch timestamp.
Hash coverage is not a claim of frame-by-frame visual or art acceptance.
Source-video keys/paths are QA-relative (Evidence/...), sampled-frame paths
are absolute, and native PNG/MP4 dimensions are included in the media index.
A run freezes its source-file list before decoding. Files arriving later are
reported for the next run and cannot enter an index without matching metadata.
"""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile


def sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest().upper()


def atomic_write(path, write):
    """A reader sees the old whole index or the new whole index, never a prefix."""
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', newline='',
                                         prefix='.' + path.name + '.', suffix='.tmp',
                                         dir=path.parent, delete=False) as stream:
            temporary = Path(stream.name)
            write(stream)
        os.replace(temporary, path)
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()


def write_json(path, value):
    atomic_write(path, lambda stream: stream.write(json.dumps(value, ensure_ascii=False, indent=2) + '\n'))


def write_csv(path, fields, rows):
    def write(stream):
        writer = csv.DictWriter(stream, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)
    atomic_write(path, write)


def png_size(path):
    if path.suffix.lower()=='.webp':
        from PIL import Image
        with Image.open(path) as picture:
            assert picture.format=='WEBP'
            return picture.size
    with path.open('rb') as stream:
        header = stream.read(24)
    if len(header) != 24 or header[:8] != b'\x89PNG\r\n\x1a\n' or header[12:16] != b'IHDR':
        raise RuntimeError('Invalid PNG header: ' + str(path))
    width, height = struct.unpack('>II', header[16:24])
    if width < 1 or height < 1:
        raise RuntimeError('Invalid PNG dimensions: ' + str(path))
    return width, height


def source_media_files(evidence):
    return sorted((p for p in evidence.rglob('*') if p.is_file() and p.suffix.lower() in ('.png', '.mp4')),
                  key=lambda p: p.relative_to(evidence).as_posix().casefold())


def completed_frames(directory, marker, identity):
    if not marker.is_file():
        raise RuntimeError('Missing completed decode; partial output is not indexed: ' + str(directory))
    complete = json.loads(marker.read_text(encoding='utf-8-sig'))
    if complete.get('sourceSha256') != identity['sourceSha256'] or complete.get('sampleFPS') != identity['sampleFPS']:
        raise RuntimeError('Decode identity changed; choose a new frame directory: ' + str(directory))
    frames = []
    extension=identity.get('frameExtension','png')
    for path in directory.glob('frame-*.'+extension):
        match = re.fullmatch(r'frame-(\d+)\.'+extension, path.name)
        if not match:
            raise RuntimeError('Unexpected sampled frame name: ' + str(path))
        frames.append((int(match.group(1)), path))
    frames.sort(key=lambda row: row[0])
    if not frames or len(frames) != complete.get('frames') or any(number != i for i, (number, _) in enumerate(frames, 1)):
        raise RuntimeError('Incomplete/noncontiguous decode; refusing to treat it as complete: ' + str(directory))
    return frames


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folder', type=Path)
    parser.add_argument('frame_root', type=Path)
    parser.add_argument('--index-only', action='store_true')
    parser.add_argument('--lossless-webp', action='store_true', help='Lossless RGB WebP frames for limited local disk; originals remain untouched')
    parser.add_argument('--png-compression', type=int, default=1, choices=range(10), help='Lossless PNG compression effort; default preserves historical method')
    parser.add_argument('--spill-frame-root', type=Path, help='Optional second authorized local directory; keeps completed frames in place')
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    folder = args.folder.resolve()
    if not folder.is_relative_to(root / 'Docs' / 'Verification') or not folder.is_dir():
        raise ValueError('QA folder must exist inside this project Docs/Verification')
    evidence = folder / 'Evidence'
    if not evidence.is_dir():
        raise FileNotFoundError(evidence)
    if not args.frame_root.is_absolute():
        raise ValueError('Frame root must be an explicit absolute local path')
    frame_root = args.frame_root.resolve()
    if frame_root.is_relative_to(evidence):
        raise ValueError('Decoded frames must stay outside Evidence to avoid mixing them with source PNGs')
    if not args.index_only:
        frame_root.mkdir(parents=True, exist_ok=True)
    frame_roots=[frame_root]
    if args.spill_frame_root:
        if not args.spill_frame_root.is_absolute():raise ValueError('Spill frame directory must be absolute')
        spill=args.spill_frame_root.resolve()
        if spill.is_relative_to(evidence):raise ValueError('Spill frames must stay outside original media')
        if not args.index_only:spill.mkdir(parents=True,exist_ok=True)
        frame_roots.append(spill)
    # Captures may still be pulled into Evidence while existing clips decode.
    # Both the decoding jobs and the eventual source-media rows use this same
    # snapshot, so a later MP4 cannot bypass metadata/decode preparation.
    source_files = source_media_files(evidence)
    videos = sorted((p for p in source_files if p.suffix.lower() == '.mp4'),key=lambda p:p.stat().st_size,reverse=True)
    sys.path.insert(0, str(root / 'Tools' / 'PythonModules'))
    import imageio_ffmpeg
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    metadata, jobs, frame_rows = {}, [], []
    for video in videos:
        source = video.relative_to(folder).as_posix()
        relative_video = video.relative_to(evidence)
        fps = 10 if video.stem.casefold().startswith(('shop-closeup', 'initial-flow', 'fishing-motion')) else 2
        # The relative source folder distinguishes identically named clips.
        extension='webp' if args.lossless_webp else 'png'
        leaf=relative_video.parent / (video.stem + '-frames-' + str(fps) + 'fps'+('-lossless-webp' if args.lossless_webp else ''))
        existing=[base/leaf for base in frame_roots if (base/leaf).exists()]
        if len(existing)>1:raise RuntimeError('Ambiguous duplicate frame directories: '+str(existing))
        directory=existing[0] if existing else max(frame_roots,key=lambda p:shutil.disk_usage(p).free)/leaf
        marker = directory / 'decode-complete.json'
        initial_stat = video.stat()
        source_sha = sha256(video)
        reader = imageio_ffmpeg.read_frames(str(video))
        try:
            data = next(reader)  # Header/metadata only; do not consume video pixels here.
        finally:
            reader.close()
        data['sha256'], data['bytes'] = source_sha, initial_stat.st_size
        data['sampleFPS'] = fps
        metadata[source] = data
        identity = {'sourceVideo': str(video), 'sourceSha256': source_sha, 'sampleFPS': fps,
                    'operation': 'ffmpeg FPS sampling, native encoded resolution/orientation, lossless decoded RGB pixels', 'frameExtension':extension}
        if not marker.exists() and not args.index_only:
            directory.mkdir(parents=True, exist_ok=True)
            if any(directory.iterdir()):
                raise RuntimeError('Partial output exists; no files overwritten. Inspect it or choose a fresh frame root: ' + str(directory))
            print('Decoding ' + source + ' at ' + str(fps) + ' FPS -> ' + str(directory), flush=True)
            encoding=['-c:v','libwebp','-lossless','1','-compression_level','6','-quality','100'] if args.lossless_webp else ['-compression_level',str(args.png_compression)]
            subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-nostdin', '-n',
                            '-threads', '2', '-noautorotate', '-i', str(video), '-vf', 'fps=' + str(fps),
                            '-threads', '2', '-pix_fmt','rgb24', *encoding, str(directory / ('frame-%05d.'+extension))], check=True)
            final_stat = video.stat()
            if (final_stat.st_size, final_stat.st_mtime_ns) != (initial_stat.st_size, initial_stat.st_mtime_ns):
                raise RuntimeError('Source video changed during decoding; output remains uncompleted: ' + str(video))
            identity['frames'] = len(list(directory.glob('frame-*.'+extension)))
            if identity['frames'] < 1:
                raise RuntimeError('Decoder produced no frames: ' + str(video))
            write_json(marker, identity)
        frames = completed_frames(directory, marker, identity)
        native_size = tuple(data.get('source_size') or data['size'])
        for number, frame in frames:
            width, height = png_size(frame)
            if (width, height) != native_size:
                raise RuntimeError('Sampled frame dimensions differ from native source: ' + str(frame))
            frame_rows.append({'path': str(frame), 'sourceVideo': source, 'sampleFPS': fps,
                               'approxOutputPTS': (number - 1) / fps, 'bytes': frame.stat().st_size,
                               'sha256': sha256(frame), 'width': width, 'height': height})
        jobs.append({'sourceVideo': source, 'sourceSha256': source_sha, 'absoluteFrameDirectory': str(directory),
                     'completionMarker': str(marker), 'sampleFPS': fps, 'frames': len(frames)})
    media_rows = []
    for path in source_files:
        relative = path.relative_to(folder).as_posix()
        if path.suffix.lower() == '.png':
            width, height = png_size(path)
            digest = sha256(path)
        else:
            data = metadata[relative]
            width, height = data.get('source_size') or data['size']
            digest = data['sha256']
        media_rows.append({'path': relative, 'bytes': path.stat().st_size, 'sha256': digest, 'width': width, 'height': height})
    media_rows.extend({key: row[key] for key in ('path', 'bytes', 'sha256', 'width', 'height')} for row in frame_rows)
    snapshot_paths = set(source_files)
    deferred = [p.relative_to(folder).as_posix() for p in source_media_files(evidence) if p not in snapshot_paths]
    if deferred:
        print('Deferred ' + str(len(deferred)) + ' newly arrived source media files until the next run; see local-frame-storage.json.', flush=True)
    storage = {'candidate': folder.name, 'absoluteFrameRoot': str(frame_root), 'sourceMediaRemainInProject': str(evidence),
               'externalUpload': False, 'samplingRule': 'shop-closeup*, initial-flow* and fishing-motion* basenames 10FPS, all other MP4 basenames 2FPS',
               'sourceEnumerationNote': 'Source files are frozen at run start. New arrivals are excluded from this run and picked up on rerun.',
               'sourceSnapshotCount': len(source_files), 'deferredSourceFiles': deferred,
               'samplingTimeNote': 'approxOutputPTS is output sampling time, not an exact source frame or touch timestamp.',
               'integrityNote': 'File hash coverage is not frame-by-frame visual inspection or art acceptance.', 'jobs': jobs}
    storage['frameFormat']='lossless RGB WebP' if args.lossless_webp else 'lossless PNG'
    storage['absoluteFrameRoots']=[str(p) for p in frame_roots]
    # All hashes and row sets are complete before any public index is replaced.
    write_json(folder / 'video-metadata.json', metadata)
    write_json(folder / 'local-frame-storage.json', storage)
    write_csv(folder / 'frame-index.csv', ['path', 'sourceVideo', 'sampleFPS', 'approxOutputPTS', 'bytes', 'sha256', 'width', 'height'], frame_rows)
    write_csv(folder / 'media-index.csv', ['path', 'bytes', 'sha256', 'width', 'height'], media_rows)
    print(json.dumps({'candidate': folder.name, 'videoCount': len(videos), 'frameCount': len(frame_rows),
                      'mediaCount': len(media_rows), 'deferredMediaCount': len(deferred), 'frameRoot': str(frame_root)}, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
