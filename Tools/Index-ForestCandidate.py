"""Decode unchanged local runtime videos and index original evidence by SHA256.

Frames may live in a second explicitly supplied local writable directory. No
resize, crop, compositing, painting or project upload is performed.
"""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest().upper()


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('folder', type=Path)
    ap.add_argument('frame_root', type=Path)
    ap.add_argument('--index-only', action='store_true')
    args = ap.parse_args()
    root = Path(__file__).resolve().parent.parent
    folder = args.folder.resolve()
    evidence = folder / 'Evidence'
    frame_root = args.frame_root.resolve()
    frame_root.mkdir(parents=True, exist_ok=True)
    sys.path.insert(0, str(root / 'Tools' / 'PythonModules'))
    import imageio_ffmpeg
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    jobs = [
        ('visits-1.mp4', 'Visits1Frames2FPS', 2),
        ('visits-2.mp4', 'Visits2Frames2FPS', 2),
        ('interactions.mp4', 'InteractionFrames2FPS', 2),
        ('Long/region-flow.mp4', 'LongFlowFrames2FPS', 2),
        ('Narrow/region-flow.mp4', 'NarrowFlowFrames2FPS', 2),
        ('visits-1.mp4', 'DenseVisits1full10FPS', 10),
        ('visits-2.mp4', 'DenseVisits2full10FPS', 10),
    ]
    for n in (1, 2):
        source = 'shop-closeup-' + str(n) + '.mp4'
        if (evidence / source).exists():
            jobs.append((source, 'ShopCloseup' + str(n) + 'full10FPS', 10))
    metadata = {}
    for source, name, fps in jobs:
        video = evidence / source
        if not video.is_file():
            raise FileNotFoundError(video)
        if source not in metadata:
            reader = imageio_ffmpeg.read_frames(str(video))
            metadata[source] = next(reader)
            reader.close()
            metadata[source]['sha256'] = sha(video)
        destination = frame_root / name
        completion = destination / 'decode-complete.json'
        identity = {'sourceVideo': str(video), 'sourceSha256': metadata[source]['sha256'], 'sampleFPS': fps,
                    'operation': 'ffmpeg fps sampling, native encoded resolution, unchanged decoded PNG pixels'}
        if not args.index_only and not completion.exists():
            destination.mkdir(exist_ok=True)
            if list(destination.glob('frame-*.png')):
                raise RuntimeError('Partial output exists; inspect before retrying: ' + str(destination))
            print('Decoding ' + source + ' at ' + str(fps) + ' FPS to ' + str(destination), flush=True)
            subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-threads', '2', '-i', str(video),
                            '-vf', 'fps=' + str(fps), '-threads', '2', '-compression_level', '1',
                            str(destination / 'frame-%05d.png')], check=True)
            identity['frames'] = len(list(destination.glob('frame-*.png')))
            write_json(completion, identity)
        if not completion.exists():
            raise RuntimeError('Missing completed decode: ' + str(destination))
        complete = json.loads(completion.read_text(encoding='utf-8'))
        if complete['sourceSha256'] != identity['sourceSha256'] or complete['sampleFPS'] != fps:
            raise RuntimeError('Stale decode identity: ' + str(destination))
    write_json(folder / 'video-metadata.json', metadata)
    write_json(folder / 'local-frame-storage.json', {
        'candidate': folder.name, 'absoluteFrameRoot': str(frame_root),
        'reason': 'Same-machine second authorized writable workspace preserves Unity build-disk headroom.',
        'videosRemainInProject': str(evidence), 'externalUpload': False,
        'samplingTimeNote': 'approxOutputPTS is output sampling time, not an exact input frame or touch timestamp.',
        'jobs': [{'sourceVideo': 'Evidence/' + s, 'absoluteFrameDirectory': str(frame_root / n), 'sampleFPS': f}
                 for s, n, f in jobs]})
    frame_rows = []
    for source, name, fps in jobs:
        for n, frame in enumerate(sorted((frame_root / name).glob('frame-*.png'))):
            frame_rows.append({'path': str(frame), 'sourceVideo': 'Evidence/' + source, 'sampleFPS': fps,
                               'approxOutputPTS': n / fps, 'bytes': frame.stat().st_size, 'sha256': sha(frame)})
    with (folder / 'frame-index.csv').open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=['path', 'sourceVideo', 'sampleFPS', 'approxOutputPTS', 'bytes', 'sha256'])
        writer.writeheader()
        writer.writerows(frame_rows)
    media_rows = []
    files = [(p, p.relative_to(folder).as_posix(), None) for p in sorted(evidence.rglob('*'))
             if p.is_file() and p.suffix.lower() in ('.png', '.mp4')]
    files += [(Path(row['path']), row['path'], row['sha256']) for row in frame_rows]
    for path, display_path, digest in files:
        width = height = ''
        if path.suffix.lower() == '.png':
            with path.open('rb') as stream:
                header = stream.read(24)
            if header[:8] != b'\x89PNG\r\n\x1a\n':
                raise RuntimeError('Invalid PNG: ' + str(path))
            width, height = struct.unpack('>II', header[16:24])
        media_rows.append({'path': display_path, 'bytes': path.stat().st_size, 'sha256': digest or sha(path),
                           'width': width, 'height': height})
    with (folder / 'media-index.csv').open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=['path', 'bytes', 'sha256', 'width', 'height'])
        writer.writeheader()
        writer.writerows(media_rows)
    print(json.dumps({'frameCount': len(frame_rows), 'mediaCount': len(media_rows), 'frameRoot': str(frame_root)}, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
