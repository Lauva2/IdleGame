"""Original resin pine, authored in a fresh Blender background process.
Reuses only the four named mesh helpers from Make-ForestTrees.py; never
executes its six-tree export or modifies the existing ForestTrees.blend.
"""
import bpy, math, random, struct, json, ast
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Assets/Resources/Models'
SOURCE = ROOT/'ArtSource'
for obj in list(bpy.data.objects): bpy.data.objects.remove(obj, do_unlink=True)
palette = [(.22,.36,.27,1),(.31,.46,.32,1),(.43,.55,.36,1),(.53,.62,.43,1)]
source = ast.parse((ROOT/'Tools/Make-ForestTrees.py').read_text(encoding='utf8'))
helpers = ast.Module(body=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ('mesh','branch','crown','export')], type_ignores=[])
exec(compile(helpers,'original-forest-tree-helpers','exec'),globals())
col = bpy.data.collections.new('ResinPine'); bpy.context.scene.collection.children.link(col)
branch('Pine root flare and bent trunk',[(0,0,0),(.10,.04,.35),(.07,.08,2),(.18,.13,3.7),(.08,.12,5.3),(.22,.1,6.7)],[.53,.43,.34,.23,.13,.02],col)
for i in range(6):
    a=i*math.tau/6
    branch('Spreading pine root %02d'%i,[(0,0,.35),(math.cos(a)*.7,math.sin(a)*.7,.13),(math.cos(a)*1.4,math.sin(a)*1.2,.03)],[.22,.12,.015],col)
for tier in range(4):
    height=3.7+tier*.78; radius=1.75-tier*.34
    for side in range(3):
        a=side*math.tau/3+tier*.85; x=math.cos(a)*radius*.48; y=math.sin(a)*radius*.48
        branch('Pine bough %02d %02d'%(tier,side),[(.10,.12,height-.6),(x*.6,y*.6,height-.15),(x,y,height+.05)],[.12-tier*.02,.065,.015],col)
        crown('Soft needle cluster %02d %02d'%(tier,side),(x,y,height),(radius*.82,radius*.70,.47+tier*.02),700+tier*10+side,col)
crown('Pine terminal crown',(.2,.1,6.7),(.52,.49,.66),799,col)
manifest=export(col,'ResinPine')
mat=bpy.data.materials.new('Original soft pine paint'); mat.use_nodes=True
bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'); bsdf.inputs['Roughness'].default_value=.88
vertex=mat.node_tree.nodes.new('ShaderNodeVertexColor'); vertex.layer_name='Forest paint'; mat.node_tree.links.new(vertex.outputs['Color'],bsdf.inputs['Base Color'])
for obj in col.objects: obj.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ForestResinPine.blend'))
(SOURCE/'ForestResinPine-manifest.json').write_text(json.dumps({'tool':'Blender '+bpy.app.version_string,'original':True,'format':'FSTM Unity Y-up, vertex sRGB, reversed winding','meshes':[manifest]},ensure_ascii=False,indent=2),encoding='utf8')
print('FOREST_RESIN_PINE_EXPORTED',manifest)
