"""Read the bundled font cmap directly; no external dependency or font modification."""
from pathlib import Path
import struct
import re

root = Path(__file__).resolve().parents[1]
font = (root / 'Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf').read_bytes()
u16 = lambda p: struct.unpack_from('>H', font, p)[0]
u32 = lambda p: struct.unpack_from('>I', font, p)[0]
tables = {}
for i in range(u16(4)):
    p = 12 + i * 16
    tag = font[p:p+4].decode('ascii')
    tables[tag] = (u32(p+8), u32(p+12))
cmap = tables['cmap'][0]
ranges = []
for i in range(u16(cmap+2)):
    p = cmap + 4 + i * 8
    start = cmap + u32(p+4)
    if u16(start) == 12:
        for g in range(u32(start+12)):
            q = start + 16 + g * 12
            ranges.append((u32(q), u32(q+4)))
        break
if not ranges:
    raise RuntimeError('Required Unicode cmap format 12 was not found')
source = '\n'.join(p.read_text(encoding='utf-8') for p in (root / 'Assets/Scripts').rglob('*.cs'))
chars = sorted(set(re.findall(r'[^\x00-\x7f\s]', source)))
missing = [c for c in chars if not any(a <= ord(c) <= b for a, b in ranges)]
output = f'Bundled font: NotoSansCJKsc-Regular.otf\nUnique non-ASCII source characters: {len(chars)}\nMissing glyphs: {len(missing)}\n' + ''.join(missing) + '\nThis static cmap audit does not replace runtime Chinese layout verification.\n'
(root / 'Docs/Verification/dev01/font-coverage.txt').write_text(output, encoding='utf-8')
print(output)
if missing:
    raise SystemExit(1)
