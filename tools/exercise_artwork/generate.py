#!/usr/bin/env python3
"""Original Core-Up joint-based artwork. Requires Pillow; no network or model calls.
Run from any directory. PNGs are rendered at 2x and downsampled for clean edges.
Coordinates are orthographic illustrations, not motion capture or clinical validation.
"""
from pathlib import Path
import argparse
import copy
import hashlib
import json
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mobile/assets/exercises'
INK = '#25364d'
BLUE = '#147bff'
SKIN = '#e5ebf2'
PANTS = '#344c70'
SCALE = 2


def pose(s=(270, 559), h=(420, 560), head=(204, 560), legs=None, arms=None, **kw):
    return dict(s=s, h=h, head=head,
                legs=legs or [[(482, 424), (560, 570), (584, 570)],
                              [(502, 440), (590, 570), (615, 570)]],
                arms=arms or [[(322, 567), (389, 569)],
                              [(320, 588), (388, 589)]], **kw)


def changed(p, **kw):
    q = copy.deepcopy(p)
    q.update(kw)
    return q


# Each leg is hip -> knee -> ankle -> toe; each arm shoulder -> elbow -> hand.
# Far-side joints offset slightly for an oblique side view. Front view is used
# only for fallout, consistently across its frames, to show lateral opening.
base = pose()
table = [[(429, 406), (566, 406), (590, 400)],
         [(451, 425), (587, 425), (611, 419)]]
raised_arms = [[(274, 450), (278, 370)], [(296, 466), (300, 386)]]
crunch = changed(base, s=(284, 495), head=(226, 465),
                 arms=[[(342, 517), (400, 529)], [(340, 540), (400, 549)]])
bike = changed(crunch, legs=table,
               arms=[[(270, 424), (226, 457)], [(302, 433), (240, 469)]])
bike_l = changed(bike, legs=[[(397, 394), (525, 414), (548, 414)],
                             [(548, 486), (671, 515), (692, 505)]],
                 arms=[[(313, 420), (239, 456)], [(331, 449), (245, 477)]])
bike_r = changed(bike, legs=[[(536, 460), (665, 489), (686, 479)],
                             [(418, 414), (546, 434), (569, 434)]],
                 arms=[[(334, 451), (240, 456)], [(320, 431), (246, 480)]])
dead = changed(base, legs=table, arms=raised_arms)
dead_l = changed(dead, legs=[table[0], [(545, 484), (669, 516), (691, 506)]],
                 arms=[[(198, 489), (122, 466)], raised_arms[1]])
dead_r = changed(dead, legs=[[(527, 465), (650, 497), (672, 487)], table[1]],
                 arms=[raised_arms[0], [(216, 504), (140, 481)]])
heel_l = changed(base, legs=[[(536, 546), (670, 570), (695, 570)], base['legs'][1]])
heel_r = changed(base, legs=[base['legs'][0], [(544, 554), (680, 570), (705, 570)]])
press_legs = [[(400, 425), (536, 425), (560, 419)],
              [(422, 444), (558, 444), (582, 438)]]
single_l = changed(base, legs=[press_legs[0], base['legs'][1]],
                   arms=[[(335, 492), (400, 425)], base['arms'][1]])
single_r = changed(base, legs=[base['legs'][0], press_legs[1]],
                   arms=[base['arms'][0], [(354, 509), (422, 444)]])
double = changed(base, legs=press_legs,
                 arms=[[(335, 492), (400, 425)], [(354, 509), (422, 444)]])
# Pelvis stays on the mat. Only the small lumbar arch / pelvic angle changes.
pelvic_a = changed(base, arch=14, pelvic_angle=-5)
pelvic_b = changed(base, arch=0, pelvic_angle=5)
plank = pose(s=(268, 414), h=(431, 454), head=(210, 396),
             arms=[[(260, 550), (170, 568)], [(282, 566), (190, 580)]],
             legs=[[(547, 488), (668, 536), (649, 575)],
                   [(557, 502), (680, 550), (661, 589)]])
knee = changed(plank, h=(414, 491),
                legs=[[(535, 557), (648, 529), (672, 516)],
                      [(550, 573), (663, 545), (687, 532)]])
side = pose(s=(270, 430), h=(425, 483), head=(213, 409),
            arms=[[(270, 577), (167, 577)], [(350, 446), (428, 460)]],
            legs=[[(542, 518), (665, 556), (690, 573)],
                  [(539, 532), (662, 570), (687, 587)]])
side_knee = changed(side, h=(416, 505),
                     legs=[[(535, 559), (648, 507), (673, 503)],
                           [(548, 573), (661, 521), (686, 517)]])
bird = pose(s=(280, 390), h=(442, 400), head=(220, 387),
            arms=[[(276, 478), (275, 563)], [(298, 492), (297, 577)]],
            legs=[[(451, 542), (575, 546), (600, 536)],
                  [(473, 556), (597, 560), (622, 550)]])
bird_l = changed(bird, arms=[[(197, 389), (113, 390)], bird['arms'][1]],
                 legs=[bird['legs'][0], [(580, 414), (704, 414), (726, 406)]])
bird_r = changed(bird, arms=[bird['arms'][0], [(216, 405), (132, 406)]],
                 legs=[[(559, 396), (683, 396), (705, 388)], bird['legs'][1]])
fall = pose(s=(384, 292), h=(384, 447), head=(384, 225), front=True,
            arms=[[(294, 356), (280, 438)], [(474, 356), (488, 438)]],
            legs=[[(343, 491), (351, 617), (354, 643)],
                  [(425, 491), (417, 617), (414, 643)]])
fall_l = changed(fall, legs=[[(282, 524), (351, 617), (354, 643)], fall['legs'][1]])
fall_r = changed(fall, legs=[fall['legs'][0], [(486, 524), (417, 617), (414, 643)]])
hollow = changed(base, s=(279, 499), head=(220, 469),
                 arms=[[(201, 469), (125, 438)], [(219, 486), (143, 455)]],
                 legs=[[(539, 485), (667, 436), (690, 427)],
                       [(551, 503), (679, 454), (702, 445)]])
reverse_a = changed(base, legs=table)
reverse_b = changed(base, h=(410, 518), legs=[[(368, 383), (498, 347), (521, 338)],
                                                            [(390, 402), (520, 366), (543, 357)]])
leg_a = changed(base, legs=[[(538, 503), (665, 454), (689, 446)],
                           [(550, 521), (677, 472), (701, 464)]])
leg_b = changed(base, legs=[[(425, 402), (430, 266), (454, 254)],
                           [(447, 420), (452, 284), (476, 272)]])
CATALOG = {
    'plank': [plank], 'side-plank': [side], 'modified-knee-plank': [knee],
    'modified-side-plank': [side_knee], 'hollow-body-hold': [hollow],
    'bird-dog': [bird, bird_l, bird_r], 'crunch': [base, crunch],
    'bicycle-crunch': [bike, bike_l, bike_r],
    'reverse-crunch': [reverse_a, reverse_b], 'leg-raise': [leg_a, leg_b],
    'dead-bug': [dead, dead_l, dead_r], 'pelvic-tilt': [pelvic_a, pelvic_b],
    'supine-heel-slide': [base, heel_l, heel_r],
    'bent-knee-fallout': [fall, fall_l, fall_r],
    'single-leg-abdominal-press': [base, single_l, single_r],
    'double-leg-abdominal-press': [base, double],
}


def render(p):
    p = copy.deepcopy(p)
    offset = 0 if p.get('front', False) else 130
    for key in ('s', 'h', 'head'):
        p[key] = (p[key][0], p[key][1] - offset)
    for key in ('legs', 'arms'):
        p[key] = [[(x, y-offset) for x, y in limb] for limb in p[key]]
    im = Image.new('RGBA', (768*SCALE, 768*SCALE))
    d = ImageDraw.Draw(im)
    def line(a, b, width, color):
        a, b = tuple(v*SCALE for v in a), tuple(v*SCALE for v in b)
        w = round(width*SCALE)
        d.line([a, b], fill=color, width=w)
        r = w/2
        for x, y in (a, b):
            d.ellipse((x-r, y-r, x+r, y+r), fill=color)
    def capsule(a, b, width, color):
        line(a, b, width+5, INK)
        line(a, b, width, color)
    def dot(c, r, color):
        x,y = c
        d.ellipse(((x-r)*SCALE,(y-r)*SCALE,(x+r)*SCALE,(y+r)*SCALE), fill=color)
    s,h = p['s'],p['h']
    front = p.get('front', False)
    # A single unobtrusive mat fixes the floor across all frames of an exercise.
    mat_y = (654 if front else 594) - offset
    if front:
        d.polygon([(250*SCALE,174*SCALE),(518*SCALE,174*SCALE),
                   (550*SCALE,675*SCALE),(218*SCALE,675*SCALE)], fill='#243b52')
    d.rounded_rectangle((54*SCALE,mat_y*SCALE,738*SCALE,(mat_y+12)*SCALE),
                        radius=6*SCALE, fill='#375673')
    def origins(i):
        if front:
            return ((s[0]+(-48 if i==0 else 48),s[1]+15),
                    (h[0]+(-28 if i==0 else 28),h[1]))
        return ((s[0]+i*15,s[1]+i*15),(h[0]+i*12,h[1]+i*10))
    def leg(i):
        _, hip=origins(i)
        k,a,t = p['legs'][i]
        color = '#526c91' if i==0 else PANTS
        capsule(hip,k,38,color)
        capsule(k,a,29,color)
        capsule(a,t,22,SKIN)
        dot(k,7,'#8198b4')
    def arm(i):
        shoulder,_=origins(i)
        e,w=p['arms'][i]
        capsule(shoulder,e,24,SKIN)
        capsule(e,w,20,SKIN)
        dot(e,5,'#a8b8ca')
        dot(w,12,SKIN)
    # Draw distant limbs behind the torso; near limbs in front.
    leg(0)
    arm(0)
    dx,dy=h[0]-s[0],h[1]-s[1]
    length=math.hypot(dx,dy)
    nx,ny=-dy/length,dx/length
    arch=p.get('arch',0)
    # Slight lumbar contour, with pelvis anchored to avoid a bridge silhouette.
    center=((s[0]+h[0])/2,(s[1]+h[1])/2-arch)
    poly=[(s[0]+nx*32,s[1]+ny*32),
          (center[0]+nx*25,center[1]+ny*25),
          (h[0]+nx*28,h[1]+ny*28),
          (h[0]-nx*28,h[1]-ny*28),
          (center[0]-nx*23,center[1]-ny*23),
          (s[0]-nx*32,s[1]-ny*32)]
    d.polygon([(int(x*SCALE),int(y*SCALE)) for x,y in poly], fill=BLUE)
    d.line([(int(x*SCALE),int(y*SCALE)) for x,y in poly+[poly[0]]], fill=INK,width=5*SCALE,joint='curve')
    # Neck is behind the featureless oval head, no face/hair.
    capsule(s,p['head'],22,SKIN)
    # Shirt covers base of neck.
    capsule(s,(s[0]+dx*.22,s[1]+dy*.22),51,BLUE)
    capsule((h[0]-dx*.08,h[1]-dy*.08),h,46,PANTS)
    if 'pelvic_angle' in p:
        a=p['pelvic_angle']
        line((h[0]-20,h[1]-16+a),(h[0]+17,h[1]-16-a),4,'#90b8e8')
    leg(1)
    arm(1)
    dot(p['head'],31,INK)
    dot(p['head'],28,SKIN)
    return im.resize((768,768),Image.Resampling.LANCZOS)


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--review-dir', type=Path)
    args=parser.parse_args()
    manifest={}
    for slug, poses in CATALOG.items():
        folder=OUT/slug
        folder.mkdir(parents=True,exist_ok=True)
        paths=[]
        hashes=[]
        for i,p in enumerate(poses):
            path=folder/('static.png' if len(poses)==1 else f'frame_{i+1}.png')
            render(p).save(path,optimize=True)
            with Image.open(path) as im:
                im.verify()
            with Image.open(path) as im:
                assert im.size==(768,768) and im.mode=='RGBA'
                alpha=im.getchannel('A')
                assert alpha.getextrema()==(0,255)
                bbox=alpha.getbbox()
                assert bbox[0]>0 and bbox[1]>0 and bbox[2]<768 and bbox[3]<768
            paths.append(str(path.relative_to(ROOT/'mobile')))
            hashes.append(hashlib.sha256(path.read_bytes()).hexdigest())
        assert len(set(hashes))==len(hashes),slug
        manifest[slug]=dict(type='static' if len(poses)==1 else 'dynamic',
                            frames=paths,sequence=[0] if len(poses)==1 else [0,1] if len(poses)==2 else [0,1,0,2],
                            sha256=hashes)
    (Path(__file__).parent/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    registry = """// Generated by tools/exercise_artwork/generate.py. Do not edit paths by hand.
// Indices are illustrative phases, never repetition counts.
class ExerciseArtwork {
  const ExerciseArtwork({required this.frames, required this.sequence});
  final List<String> frames;
  final List<int> sequence;
  bool get isStatic => frames.length == 1;
}

const coreArtwork = <String, ExerciseArtwork>{
"""
    for slug, item in manifest.items():
        registry += f"  '{slug}': ExerciseArtwork(\n    frames: [\n"
        registry += ''.join(f"      '{path}',\n" for path in item['frames'])
        registry += f"    ],\n    sequence: {item['sequence']},\n  ),\n"
    registry += '};\n'
    (ROOT/'mobile/lib/widgets/workout/exercise_artwork.dart').write_text(registry)
    if args.review_dir:
        args.review_dir.mkdir(parents=True,exist_ok=True)
        font=ImageFont.truetype('/System/Library/Fonts/Menlo.ttc',18)
        for page in range(2):
            sheet=Image.new('RGB',(1152,8*238),'#111925')
            d=ImageDraw.Draw(sheet)
            for row,(slug,poses) in enumerate(list(CATALOG.items())[page*8:(page+1)*8]):
                y=row*238
                d.text((18,y+10),slug,font=font,fill='#f7f9fc')
                for col,p in enumerate(poses):
                    thumb=render(p).resize((212,212),Image.Resampling.LANCZOS)
                    sheet.paste(thumb,(col*360+60,y+22),thumb)
                    d.text((col*360+20,y+198),str(col+1),font=font,fill='#a7b0be')
            sheet.save(args.review_dir/f'core-exercises-{page+1}.png')
        (args.review_dir/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(f'{len(CATALOG)} exercises; {sum(map(len,CATALOG.values()))} RGBA PNGs; metadata, margins and per-exercise uniqueness checked.')

if __name__=='__main__':
    main()
