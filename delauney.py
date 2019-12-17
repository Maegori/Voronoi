import random
import cv2
import numpy as np
import math

def calculate_aspect(filler):
    x = (_WIDTH + filler)
    y = (_HEIGHT + filler)

    return (round((_DENS * x) / 100), round((_DENS * y) / 100))

def painter(img, mat):
    
    r = (0, 0, _HEIGHT + _AR[1], _WIDTH + _AR[0])

    subdiv = cv2.Subdiv2D(r)


    for i in range(len(mat)):
        for j in range(len(mat[i])):
            point = mat[i][j]
            cv2.circle(img, (point[1], point[0]), 8, (0, 0, 0), -1)

    for i in range(len(mat)):
        for j in range(len(mat[i])):
            point = mat[i][j]
            subdiv.insert((point[1], point[0]))

    tlist = subdiv.getTriangleList()
    print(len(tlist))

    for t in tlist :
         
        pt1 = (t[0], t[1])
        pt2 = (t[2], t[3])
        pt3 = (t[4], t[5])
    
        cv2.line(img, pt1, pt2, (0, 0, 0), 2, 0)
        cv2.line(img, pt2, pt3, (0, 0, 0), 2, 0)
        cv2.line(img, pt3, pt1, (0, 0, 0), 2, 0)

    return img

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

_WIDTH = 1920
_HEIGHT = 1080
_DENS = 30

_AR = calculate_aspect(100)
_XLEN = round(_WIDTH / _AR[0])
_YLEN = round(_HEIGHT / _AR[1])

img = 255 * np.ones(shape=[_WIDTH, _HEIGHT, 3], dtype=np.uint8)
mat = point_generator()
newimg = painter(img, mat)
newimg = cv2.rotate(img, cv2.ROTATE_90_COUNTERCLOCKWISE)
cv2.imwrite("temp.png", newimg)
