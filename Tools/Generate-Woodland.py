"""Original local sculpted meshes for the forest diorama. No external models.

Smooth implicit volumes are sampled offline so mobile startup only reads meshes.
FSTM v1: magic, vertex/index counts, positions, normals, RGBA bytes, uint indices.
"""
from pathlib import Path
import struct
import numpy as np

OUT = Path(__file__).resolve().parents[1] / "Assets/Resources/Models"
OUT.mkdir(parents=True, exist_ok=True)


def smooth_min(a, b, k):
    h = np.maximum(k - np.abs(a - b), 0) / k
    return np.minimum(a, b) - h * h * k * .25


def ellipsoid(p, center, radius):
    q = (p - center) / radius
    return (np.sqrt((q * q).sum(axis=-1)) - 1) * min(radius)


def branch(p, a, b, ra, rb):
    a, b = np.array(a), np.array(b)
    d = b - a
    t = np.clip(((p - a) * d).sum(axis=-1) / np.dot(d, d), 0, 1)
    return np.sqrt(((p - a - t[..., None] * d) ** 2).sum(axis=-1)) - (ra + (rb - ra) * t)


def trunk(p):
    paths = [
        ([(0, .1, 2.1), (-.25, 3, 2.15), (.05, 6.7, 2.25), (-.4, 9.6, 2.5), (-.9, 12.3, 2.8), (-1.3, 13.3, 3)], [1.9, 1.35, 1.12, .77, .36, .09]),
        ([(-.2, 8.4, 2.35), (1.0, 9.8, 2.5), (2.1, 11.3, 2.6), (2.7, 12.5, 2.9)], [.72, .58, .33, .08]),
        ([(-.4, 9.1, 2.45), (-1.5, 10.15, 2.4), (-2.8, 11.3, 2.65), (-3.2, 12.1, 2.7)], [.59, .43, .23, .07]),
        ([(1.15, 10, 2.5), (2.5, 10.7, 3), (3.4, 11.9, 3.4)], [.39, .25, .07]),
        ([(-.6, 10.5, 2.7), (-.3, 11.9, 3.55), (.2, 13.1, 4)], [.43, .27, .08]),
    ]
    field = np.full(p.shape[:-1], 100.)
    for points, radii in paths:
        for i in range(len(points) - 1):
            field = smooth_min(field, branch(p, points[i], points[i+1], radii[i], radii[i+1]), .42)
    for i in range(9):
        a = i * np.pi * 2 / 9 + .2
        root = [(np.sin(a)*3.4, .05, 2.1+np.cos(a)*2.9), (np.sin(a)*1.65, .3, 2.1+np.cos(a)*1.5), (np.sin(a)*.6, 1.5, 2.1+np.cos(a)*.7)]
        for j in range(2):
            field = smooth_min(field, branch(p, root[j], root[j+1], [.1, .38][j], [.38, .63][j]), .35)
    angle = np.arctan2(p[..., 2] - 2.2, p[..., 0] + .15)
    return field + .025*np.sin(angle*11 + p[..., 1]*.55)


# Seven connected masses establish an asymmetric oak silhouette; the smaller
# lobes belong to those masses instead of reading as separately stacked discs.
LOBES = [
    ((-.6, 12.8, 2.8), (2.05, 1.65, 1.8)),
    ((-2.35, 12, 2.5), (1.9, 1.45, 1.55)),
    ((2, 12.25, 2.9), (2.05, 1.6, 1.65)),
    ((-.8, 14, 3.05), (1.55, 1.27, 1.4)),
    ((-3.6, 11.55, 2.6), (1.3, 1.05, 1.15)),
    ((3.6, 11.55, 3.1), (1.35, 1.18, 1.2)),
    ((.45, 11.45, 1.5), (1.85, 1.18, 1.35)),
]
rng = np.random.default_rng(1042)
for center, radius in list(LOBES):
    center, radius = np.array(center), np.array(radius)
    for i in range(3):
        v = rng.normal(size=3)
        v /= np.linalg.norm(v)
        at = center + v * radius * .80
        size = rng.uniform(.56, .76)
        LOBES.append((tuple(at), (size*1.1, size*.9, size)))


def crown(p):
    field = np.full(p.shape[:-1], 100.)
    for center, radius in LOBES:
        field = smooth_min(field, ellipsoid(p, center, radius), .42)
    return field


def crown_color(p):
    light = np.clip((p[:, 1]-10.3)/5, 0, 1)
    a, b = np.array([84, 120, 73]), np.array([151, 168, 97])
    patches = .06*np.sin(p[:,0]*1.15+p[:,1]*.7) + .035*np.cos(p[:,2]*2-p[:,1])
    t = np.clip(light*.8 + .08 + patches, 0, 1)[:, None]
    return a*(1-t)+b*t


def bark_color(p):
    a = np.arctan2(p[:, 2]-2.2, p[:, 0]+.15)
    stripes = (.5+.5*np.sin(a*13+p[:, 1]*.9))**5
    t = (.32 + .25*np.sin(a*4-p[:, 1]*.4) - .24*stripes)[:, None]
    return np.array([113, 77, 45]) + t*np.array([62, 53, 33])


def save_leaf_relief():
    """Shallow solid oak leaves laid along the real crown surface, not billboards."""
    vertices, normals, colors, faces = [], [], [], []
    rng = np.random.default_rng(733)
    outline = 24
    def seat(points, offset):
        points = np.array(points)
        for _ in range(6):
            gradient = np.stack([crown(points+np.eye(3)[k]*.008)-crown(points-np.eye(3)[k]*.008) for k in range(3)], axis=-1)/.016
            points -= (crown(points)/np.maximum((gradient*gradient).sum(axis=-1),1e-5))[:,None]*gradient
        normal = gradient/np.maximum(np.linalg.norm(gradient,axis=-1,keepdims=True),1e-5)
        return points+normal*offset, normal
    # Small overlapping sprays follow the seven crown masses, leaving broad
    # quiet areas between them instead of a uniform dotted surface pattern.
    clusters = [(-3.7,11.8,8),(-2.55,12.45,13),(-1.1,13.0,12),
                (-.65,14.15,10),(.0,11.7,12),(1.8,12.8,14),(3.45,11.75,10)]
    for row, (cx, cy, count) in enumerate(clusters):
        for leaf in range(count):
            angle = rng.uniform(0,np.pi*2)
            radius = np.sqrt(rng.uniform(0,1))*.78
            xj, yj = cx+np.cos(angle)*radius, cy+np.sin(angle)*radius*.8
            ray = np.array([(xj, yj, z) for z in np.arange(-.5, 5.1, .16)])
            hits = np.flatnonzero(crown(ray) < 0)
            if not len(hits) or hits[0] == 0:
                continue
            lo, hi = ray[hits[0]-1].copy(), ray[hits[0]].copy()
            for _ in range(10):
                middle = (lo+hi)*.5
                if crown(middle) < 0: hi = middle
                else: lo = middle
            at = (lo+hi)*.5
            eps = .015
            normal = np.array([crown(at+np.eye(3)[i]*eps)-crown(at-np.eye(3)[i]*eps) for i in range(3)])
            normal /= np.linalg.norm(normal)
            if normal[2] > -.28: continue
            direction = np.array([np.sin(angle)*.65+xj*.10, .8+np.cos(angle)*.3, -.15])
            direction -= normal*np.dot(direction,normal)
            direction /= np.linalg.norm(direction)
            across = np.cross(direction, normal)
            length = .92 + rng.uniform(-.18,.15)
            width = .36 + rng.uniform(-.07,.05)
            base = len(vertices)
            shade = crown_color(at[None,:])[0] * rng.uniform(1.10,1.25)
            # The crown supplies the solid backing. Only the outward relief
            # surface is needed; an inward-facing thin shell intersected the
            # triangulated crown and exposed dark slivers along its rim.
            vertices.append(at+normal*.13)
            normals.append(normal); colors.append(shade)
            for ring in range(3):
                radius = (.33,.66,1.)[ring]
                rim = []
                for i in range(outline):
                    a = i*np.pi*2/outline
                    along = np.cos(a)*length*.5*radius
                    lateral = np.sin(a)*width*.5*(1+.15*np.cos(np.cos(a)*np.pi*6))*radius
                    rim.append(at+direction*along+across*lateral)
                fitted, fitted_normals = seat(rim, .08+.05*(1-radius))
                vertices.extend(fitted)
                normals.extend(fitted_normals)
                colors.extend([shade*.98]*outline)
            for i in range(outline):
                a, b = base+1+i, base+1+(i+1)%outline
                faces.append((base,a,b))
                for ring in range(2):
                    c,d=a+ring*outline,b+ring*outline
                    faces.extend([(c,c+outline,d+outline),(c,d+outline,d)])
    v, n, f = np.array(vertices), np.array(normals), np.array(faces,dtype=np.uint32)
    cross = np.cross(v[f[:,1]]-v[f[:,0]], v[f[:,2]]-v[f[:,0]])
    reverse = (cross*n[f].mean(axis=1)).sum(axis=1)<0
    f[reverse] = f[reverse][:,[0,2,1]]
    rgba = np.column_stack([np.clip(colors,0,255).astype(np.uint8),np.full(len(v),255,dtype=np.uint8)])
    with (OUT / 'OakLeafRelief.bytes').open('wb') as stream:
        stream.write(struct.pack('<4sII', b'FSTM', len(v), f.size))
        stream.write(v.astype('<f4').tobytes()); stream.write(n.astype('<f4').tobytes())
        stream.write(rgba.tobytes()); stream.write(f.astype('<u4').tobytes())
    print('OakLeafRelief leaves', len(v)//(outline*3+1), 'triangles',len(f))


def save_mesh(name, field, colors, low, high, step, compress_upper=False):
    axes = [np.arange(lo, hi+step, step) for lo, hi in zip(low, high)]
    points = np.stack(np.meshgrid(*axes, indexing="ij"), axis=-1)
    values = field(points)
    dims = values.shape
    corners = np.array([(0,0,0),(1,0,0),(1,1,0),(0,1,0),(0,0,1),(1,0,1),(1,1,1),(0,1,1)])
    cubes = [values[dx:dx+dims[0]-1, dy:dy+dims[1]-1, dz:dz+dims[2]-1] for dx,dy,dz in corners]
    active = np.argwhere((np.minimum.reduce(cubes)<0) & (np.maximum.reduce(cubes)>=0))
    tetra = [(0,5,1,6),(0,1,2,6),(0,2,3,6),(0,3,7,6),(0,7,4,6),(0,4,5,6)]
    vertices, faces, edges = [], [], {}
    flat_points, flat_values = points.reshape(-1,3), values.ravel()
    strides = np.array([dims[1]*dims[2], dims[2], 1])

    def cut(a, b):
        key = (min(a,b), max(a,b))
        if key not in edges:
            t = flat_values[a] / (flat_values[a]-flat_values[b])
            edges[key] = len(vertices)
            vertices.append(flat_points[a] + t*(flat_points[b]-flat_points[a]))
        return edges[key]

    for cell in active:
        ids = (corners + cell) @ strides
        for tet in tetra:
            inside = [int(ids[i]) for i in tet if flat_values[ids[i]] < 0]
            outside = [int(ids[i]) for i in tet if flat_values[ids[i]] >= 0]
            if len(inside) == 1:
                faces.append([cut(inside[0], b) for b in outside])
            elif len(inside) == 3:
                faces.append([cut(outside[0], b) for b in inside])
            elif len(inside) == 2:
                a,b,c,d = cut(inside[0],outside[0]),cut(inside[0],outside[1]),cut(inside[1],outside[0]),cut(inside[1],outside[1])
                faces.extend([(a,b,c),(b,d,c)])
    v, f = np.array(vertices), np.array(faces, dtype=np.uint32)
    eps = .012
    normals = np.stack([field(v+np.eye(3)[i]*eps)-field(v-np.eye(3)[i]*eps) for i in range(3)], axis=-1)
    normals /= np.maximum(np.linalg.norm(normals, axis=1, keepdims=True), 1e-8)
    # Orient faces consistently with the outward field gradient.
    crosses = np.cross(v[f[:,1]]-v[f[:,0]], v[f[:,2]]-v[f[:,0]])
    reverse = (crosses * normals[f].mean(axis=1)).sum(axis=1) < 0
    f[reverse] = f[reverse][:, [0,2,1]]
    rgba = np.column_stack([np.clip(colors(v),0,255).astype(np.uint8), np.full(len(v),255,dtype=np.uint8)])
    if compress_upper:
        upper = v[:, 1] > 8.5
        v[upper, 1] = 8.5 + (v[upper, 1] - 8.5) * .73
        normals[upper, 1] /= .73
        normals /= np.maximum(np.linalg.norm(normals, axis=1, keepdims=True), 1e-8)
    with (OUT / (name + '.bytes')).open('wb') as stream:
        stream.write(struct.pack('<4sII', b'FSTM', len(v), f.size))
        stream.write(v.astype('<f4').tobytes())
        stream.write(normals.astype('<f4').tobytes())
        stream.write(rgba.tobytes())
        stream.write(f.astype('<u4').tobytes())
    print(name, 'vertices', len(v), 'triangles', len(f), 'bounds', v.min(axis=0), v.max(axis=0))


save_mesh('OakTrunk', trunk, bark_color, (-4,-2.2,-1.6), (4,14,6), .20, compress_upper=True)
save_mesh('OakCrown', crown, crown_color, (-5.5,9,-.4), (5.5,16,5.8), .18)
save_mesh('WoodlandTrunk', trunk, bark_color, (-4,-2.2,-1.6), (4,14,6), .38)
save_mesh('WoodlandCrown', crown, crown_color, (-5.5,9,-.4), (5.5,16,5.8), .36)
save_leaf_relief()
