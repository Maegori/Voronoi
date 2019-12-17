import sys
import random
from PIL import Image, ImageDraw
import time  

def calculate_aspect(res):
    def gcd(a, b):
        return a if b == 0 else gcd(b, a % b)

    r = gcd(_WIDTH, _HEIGHT); x = _WIDTH // r; y = _HEIGHT // r
    
    #if the aspect ratio is equal to the size of the image, just return this
    if x == _WIDTH:
        return (round(100/res * 10), round(100/res * 10))

    return (round(100/res * x), round(100/res * y))

#distributes the voronoi sites evenly over the image
def point_generator():
    hc = int()
    mat = list()

    for y in range(_YLEN):
        wc = int()
        temp = []
        for x in range(_XLEN):
            rw = random.randint(wc, wc + _AR[0]); rh = random.randint(hc, hc + _AR[1])
            temp.append((rw, rh))
            wc = _AR[0] * x
        mat.append(temp)
        hc = _AR[1] * y
        
    return mat

def closest_to(pixel, mat):
    def GetDistanceSqr(p1, p2):
        dx = p1[0] - p2[0]
        dy = p1[1] - p2[1]
        return dx * dx + dy * dy

    area = (round(pixel[0] / _AR[0]), round(pixel[1] / _AR[1]))

    square = set([(c[0] + area[0], c[1] + area[1]) for c in _AROUND])
    close = set()

    for s in square:
        if s[0] >= 0 and s[0] < _XLEN and s[1] >= 0 and s[1] < _YLEN:
            close.add(mat[s[1]][s[0]])

    closest = close.pop()
    min_dist = GetDistanceSqr(closest, pixel)

    for c in close:
        dist = GetDistanceSqr(c, pixel)
        if dist < min_dist:
            min_dist = dist
            closest = c

    return closest

#collects the pixels and colors of each voronoi site
def voronoi(im, mat):
    colors = dict()
    vor = dict()

    for y in range(_HEIGHT):
        for x in range(_WIDTH):
            pixel = (x, y)
            r, g, b = im.getpixel(pixel)
            region = closest_to(pixel, mat)

            if region in vor:
                vor[region].add(pixel)
            else:
                vor[region] = set([pixel])
            
            if region in colors:
                c = colors[region]
                c[0] += r; c[1] += g; c[2] += b
                colors[region] = c
            else:
                colors[region] = [r, g, b]

    return vor, colors

#paints the pictues
def painter(img, voronoi, colors):
    paint = ImageDraw.Draw(img)

    for region in voronoi.keys():
        points = len(voronoi[region])
        color = colors[region]
        r = color[0] // points; g = color[1] // points; b = color[2] // points
        pixels = voronoi[region]
        paint.point(list(pixels), (r, g, b))
    
    return img

#setup 
t0 = time.time()
rgb_img = Image.open(sys.argv[1]).convert('RGB')
_WIDTH, _HEIGHT = rgb_img.size
_AR = calculate_aspect(int(sys.argv[2]))
_XLEN = round(_WIDTH / _AR[0])
_YLEN = round(_HEIGHT / _AR[1])
#distributes the voronoi sites over the image
mat = point_generator()
_AROUND = (
    (-1, -1),
    (0, -1),
    (1,-1),
    (-1, 0),
    (0, 0),
    (1, 0),
    (-1, 1),
    (0, 1),
    (1, 1)
)

def main():
    print("Voronoi sites:", _YLEN * _XLEN)

    #gathers the pixels and colors of each voronoi site    
    vor, colors = voronoi(rgb_img, mat)

    #creates the new image
    newimg = painter(Image.new('RGB', (_WIDTH, _HEIGHT)), vor, colors)
    newimg.show()
    newimg.save(f"output/{sys.argv[1].split('.')[0]}{sys.argv[2]}.jpg")
    t1 = time.time()
    print(f"rendering time: {round(t1 - t0, 3)} seconds")

if __name__ == "__main__":
    main()