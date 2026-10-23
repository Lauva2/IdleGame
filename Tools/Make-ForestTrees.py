"""Original d18 trees, authored with Blender. No downloads or external services.

Run: blender --background --factory-startup --python Tools/Make-ForestTrees.py
Blender Z-up is converted to Unity Y-up in our existing FSTM mesh format.
The .blend keeps the original colored mesh parts, named branches and crown lobes.
"""
import bpy, math, random, struct, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Models'
SOURCE = ROOT / 'ArtSource'
SOURCE.mkdir(exist_ok=True)
OUT.mkdir(exist_ok=True)
# This script runs in a separate factory-startup process, never the user's live file.
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
random.seed(241024)
palette = [(0.30, .39, .25, 1), (.40, .51, .29, 1), (.54, .62, .36, 1), (.66, .69, .43, 1)]

def mesh(name, verts, faces, colors, collection):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces); data.update()
    color = data.color_attributes.new(name='Forest paint', type='BYTE_COLOR', domain='POINT')
    for i, c in enumerate(colors): color.data[i].color_srgb = c
    obj = bpy.data.objects.new(name, data); collection.objects.link(obj)
    for p in data.polygons: p.use_smooth = True
    return obj

def branch(name, points, radii, collection, birch=False):
    verts=[]; faces=[]; colors=[]; sides=10
    for j, point in enumerate(points):
        p=Vector(point)
        tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        tangent.normalize(); side=tangent.cross(Vector((0,1,0))).normalized(); other=tangent.cross(side).normalized()
        for i in range(sides):
            a=i*math.tau/sides; r=radii[j]*(1+.09*math.sin(i*3+j))
            verts.append(p+r*(side*math.cos(a)+other*math.sin(a)))
            if birch:
                k=.78+.08*math.sin(i*2+j*3); c=(k,k*.96,k*.80,1)
                if j%3==1 and i in (2,3,7): c=(.33,.33,.25,1)
            else:
                k=.85+.12*math.sin(i*2.7+j*.4); c=(.48*k,.32*k,.20*k,1)
            colors.append(c)
    for j in range(len(points)-1):
        for i in range(sides):
            a=j*sides+i; b=j*sides+(i+1)%sides; faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(reversed(range(sides)))); faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    return mesh(name,verts,faces,colors,collection)

def crown(name, center, radius, seed, collection, cool=False):
    rng=random.Random(seed); verts=[]; faces=[]; colors=[]; rings=9; sides=20
    phase=rng.random()*6
    for j in range(rings+1):
        t=math.pi*j/rings; z=math.cos(t)
        for i in range(sides):
            a=i*math.tau/sides
            # Gently scalloped, broad upper plane and tapered underside.
            rim=1+.09*math.sin(5*a+phase)+.045*math.sin(9*a-phase)
            x=math.sin(t)*math.cos(a)*radius[0]*rim
            y=math.sin(t)*math.sin(a)*radius[1]*rim
            h=math.copysign(abs(z)**.8,z)*radius[2]
            verts.append((center[0]+x,center[1]+y,center[2]+h))
            u=max(0,min(2.99,(z+1)*1.45+.12*math.sin(a*3)))
            lo=int(u); f=u-lo; c=tuple(palette[lo][n]*(1-f)+palette[lo+1][n]*f for n in range(4))
            if cool: c=(c[0]*.82,c[1]*.98,c[2]*1.10,1)
            colors.append(c)
    for j in range(rings):
        for i in range(sides):
            a=j*sides+i; b=j*sides+(i+1)%sides; faces.append((a,a+sides,b+sides,b))
    mesh(name,verts,faces,colors,collection)
    # Almond leaf strokes lie on upper surface; varied, restrained ochre highlights.
    verts=[]; faces=[]; colors=[]
    for n in range(30):
        a=rng.random()*math.tau; r=rng.uniform(.23,.84); z=math.sqrt(1-r*r)
        p=Vector((center[0]+math.cos(a)*r*radius[0],center[1]+math.sin(a)*r*radius[1],center[2]+z**.8*radius[2]+.015))
        angle=a+rng.uniform(-.7,.7); length=rng.uniform(.13,.24); width=length*.40
        u=Vector((math.cos(angle),math.sin(angle),-.12)); v=Vector((-math.sin(angle),math.cos(angle),0))
        k=len(verts); verts.extend([p-u*length,p+v*width,p+u*length,p-v*width,p+Vector((0,0,.028))])
        faces.extend([(k+4,k+(i+1)%4,k+i) for i in range(4)])
        c=(.64,.69,.42,1) if n%4 else (.73,.71,.47,1)
        colors.extend([c]*5)
    mesh(name+' painted leaves',verts,faces,colors,collection)

def export(collection, asset):
    verts=[]; normals=[]; colors=[]; triangles=[]
    for obj in collection.objects:
        if obj.type!='MESH': continue
        data=obj.data; data.calc_loop_triangles(); start=len(verts)
        for v in data.vertices:
            p=obj.matrix_world@v.co; normal=obj.matrix_world.to_3x3()@v.normal
            verts.append((p.x,p.z,p.y)); normals.append((normal.x,normal.z,normal.y))
        color=data.color_attributes['Forest paint']
        colors.extend([tuple(round(max(0,min(1,c))*255) for c in p.color_srgb) for p in color.data])
        for tri in data.loop_triangles:
            # Reflection (Y/Z swap) needs the winding reversed.
            triangles.extend([start+tri.vertices[0],start+tri.vertices[2],start+tri.vertices[1]])
    path=OUT/(asset+'.bytes')
    with path.open('wb') as f:
        f.write(struct.pack('<Iii',0x4D545346,len(verts),len(triangles)))
        for p in verts: f.write(struct.pack('<fff',*p))
        for p in normals: f.write(struct.pack('<fff',*p))
        for p in colors: f.write(bytes(p))
        for i in triangles: f.write(struct.pack('<i',i))
    return {'asset':asset,'vertices':len(verts),'triangles':len(triangles)//3,'bytes':path.stat().st_size}

results=[]
for variant in range(3):
    name=['CanopyOak','SilverBirch','WoodlandLinden'][variant]
    col=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(col)
    birch=variant==1
    height=6.8 if birch else 5.1 if variant==2 else 5.8
    branch('Root flare and bending trunk',[(0,0,0),(.13,0,.3),(.18,.06,1.6),(-.03,.14,2.9),(.23,.18,4.1),(.45,.1,height-.4)], [.58,.40,.30,.23,.17,.06] if not birch else [.31,.24,.19,.15,.11,.035],col,birch)
    for j in range(5):
        a=j*math.tau/5+.3; end=(math.cos(a)*1.35,math.sin(a)*1.25,3.7+(j%2)*.55)
        branch('Fork %02d'%j,[(.05,.12,2.1+j*.19),(end[0]*.5,end[1]*.5,3.15+j*.13),end,(end[0]*1.2,end[1]*1.2,end[2]+.9)],[.18,.13,.08,.025],col,birch)
        if not birch:
            branch('Spreading root %02d'%j,[(0,0,.3),(math.cos(a)*.65,math.sin(a)*.65,.12),(math.cos(a)*1.0,math.sin(a)*1.0,.03)],[.23,.13,.015],col)
    lobes = [(0,0,height-.15,1.45,1.4,.88)]
    for j in range(7):
        a=j*math.tau/7; r=1.14 if not birch else .90
        lobes.append((math.cos(a)*r,math.sin(a)*r,height-1.65+(j%3)*.24,1.46 if not birch else 1.08,1.25 if not birch else 1.03,.92))
    for j,(x,y,z,rx,ry,rz) in enumerate(lobes):
        crown('Layered canopy %02d'%j,(x,y,z),(rx,ry,rz),variant*100+j,col,variant==1)
    results.append(export(col,name))
    # A spaced reference grove in the saved Blender source, exports stay at origin.
    for obj in col.objects: obj.location.x += (variant-1)*7

mat=bpy.data.materials.new('Original vertex-painted woodland'); mat.use_nodes=True
bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bsdf.inputs['Roughness'].default_value=.88
vertex=mat.node_tree.nodes.new('ShaderNodeVertexColor'); vertex.layer_name='Forest paint'
mat.node_tree.links.new(vertex.outputs['Color'],bsdf.inputs['Base Color'])
for obj in bpy.context.scene.objects:
    if obj.type=='MESH': obj.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ForestTrees.blend'))
(SOURCE/'ForestTrees-manifest.json').write_text(json.dumps({'tool':'Blender '+bpy.app.version_string,'original':True,'format':'FSTM Unity Y-up, vertex sRGB, reversed winding','meshes':results},ensure_ascii=False,indent=2),encoding='utf8')
print('FOREST_TREES_EXPORTED',results)
