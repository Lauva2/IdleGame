"""Read-only alpha inspection of generated atlases; does not edit images."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
image = Image.open(root / 'Assets/Resources/Art/residents-v2.png')
alpha = image.getchannel('A')
w, h = image.size
print('Atlas:', w, h)
for y0, y1 in [(0, h // 2), (h // 2, h)]:
    counts = [sum(alpha.getpixel((x, y)) > 32 for y in range(y0, y1)) for x in range(w)]
    groups = []
    start = None
    for x, count in enumerate(counts + [0]):
        if count > 4 and start is None:
            start = x
        if count <= 4 and start is not None:
            if x - start > 15:
                groups.append((start, x))
            start = None
    print('Row:', y0, y1, 'horizontal occupied regions:', groups)
