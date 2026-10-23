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
palette = [(.34, .43, .28, 1), (.43, .54, .34, 1), (.54, .63, .40, 1), (.61, .68, .46, 1)]

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
    rng=random.Random(seed); verts=[]; faces=[]; colors=[]; rings=12; sides=24
    phase=rng.random()*6
    for j in range(rings+1):
        t=math.pi*j/rings; z=math.cos(t)
        for i in range(sides):
            a=i*math.tau/sides
            # Gently scalloped, broad upper plane and tapered underside.
            rim=1+.035*math.sin(5*a+phase)+.018*math.sin(9*a-phase)
            x=math.sin(t)*math.cos(a)*radius[0]*rim
            y=math.sin(t)*math.sin(a)*radius[1]*rim
            h=z*radius[2]
            verts.append((center[0]+x,center[1]+y,center[2]+h))
            u=max(0,min(2.99,(z+1)*1.45+.12*math.sin(a*3)))
            lo=int(u); f=u-lo; c=tuple(palette[lo][n]*(1-f)+palette[lo+1][n]*f for n in range(4))
            if cool: c=(c[0]*.82,c[1]*.98,c[2]*1.10,1)
            colors.append(c)
    for j in range(rings):
        for i in range(sides):
            a=j*sides+i; b=j*sides+(i+1)%sides; faces.append((a,a+sides,b+sides,b))
    mesh(name,verts,faces,colors,collection)
    # Sparse rounded paint marks, close in value to the canopy, without sharp tips.
    verts=[]; faces=[]; colors=[]
    for n in range(7+seed%5):
        a=rng.random()*math.tau; r=rng.uniform(.23,.84); z=math.sqrt(1-r*r)
        p=Vector((center[0]+math.cos(a)*r*radius[0],center[1]+math.sin(a)*r*radius[1],center[2]+z*radius[2]+.012))
        angle=a+rng.uniform(-.7,.7); length=rng.uniform(.09,.16); width=length*.55
        u=Vector((math.cos(angle),math.sin(angle),-.12)); v=Vector((-math.sin(angle),math.cos(angle),0))
        k=len(verts); verts.append(p+Vector((0,0,.014)))
        for q in range(8):
            t=q*math.tau/8; verts.append(p+u*(math.cos(t)*length)+v*(math.sin(t)*width))
        faces.extend([(k,k+1+i,k+1+(i+1)%8) for i in range(8)])
        c=(.59,.66,.43,1) if not cool else (.48,.63,.43,1)
        colors.extend([c]*9)
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
for variant in range(6):
    name=['CanopyOak','SilverBirch','WoodlandLinden','OpenRowan','YoungBirch','LowHazel'][variant]
    col=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(col)
    birch=variant in (1,4)
    height=[5.8,8.2,6.4,6.1,5.7,3.8][variant]
    branch('Root flare and bending trunk',[(0,0,0),(.13,0,.3),(.18,.06,height*.28),(-.03,.14,height*.5),(.23,.18,height*.72),(.45,.1,height-.4)], [.58,.40,.30,.23,.17,.06] if not birch else [.31,.24,.19,.15,.11,.035],col,birch)
    for j in range(5):
        a=j*math.tau/5+.3; ratio=height/5.8; end=(math.cos(a)*1.35,math.sin(a)*1.25,(3.7+(j%2)*.55)*ratio)
        branch('Fork %02d'%j,[(.05,.12,(2.1+j*.19)*ratio),(end[0]*.5,end[1]*.5,(3.15+j*.13)*ratio),end,(end[0]*1.2,end[1]*1.2,end[2]+.9*ratio)],[.18,.13,.08,.025],col,birch)
        if not birch:
            branch('Spreading root %02d'%j,[(0,0,.3),(math.cos(a)*.65,math.sin(a)*.65,.12),(math.cos(a)*1.0,math.sin(a)*1.0,.03)],[.23,.13,.015],col)
    lobes=[]
    if birch:
        for j in range(7 if variant==1 else 4):
            a=j*2.4; z=height-3.2+j*.47; width=1.22-(z-(height-3.2))*.16
            lobes.append((math.cos(a)*.62,math.sin(a)*.62,z,width,width*.88,.9))
        lobes.append((.12,0,height,.64,.67,1.05))
    elif variant==2:
        lobes=[(0,0,height-1,1.95,1.65,1.8),(-1.2,.2,height-2,1.3,1.2,1.3),(.9,-.65,height-1.7,1.5,1.35,1.4)]
    elif variant==3:
        lobes=[(-1.9,-.25,4.5,1.4,1.1,.95),(.8,.9,5.5,1.4,1.35,1.05),(1.7,-.7,4.4,1.45,1.0,.85),(-.5,-.75,6.0,1.2,1.2,.85)]
    elif variant==5:
        lobes=[(-.9,0,2.4,1.5,1.2,1.05),(.8,.3,3,1.4,1.4,1.1),(.2,-.9,2.2,1.6,1.2,.9)]
    else:
        lobes=[(0,.3,height-.4,1.65,1.5,1.1),(-1.5,0,height-1.3,1.55,1.4,1.0),(1.1,-1,height-1.8,1.7,1.4,1.05),(.5,1.3,height-1.7,1.8,1.25,.95),(-.8,-1.25,height-1.8,1.4,1.5,.95)]
    for j,(x,y,z,rx,ry,rz) in enumerate(lobes):
        crown('Layered canopy %02d'%j,(x,y,z),(rx,ry,rz),variant*100+j,col,birch)
    results.append(export(col,name))
    # A spaced reference grove in the saved Blender source, exports stay at origin.
    for obj in col.objects: obj.location.x += (variant-2.5)*7

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
