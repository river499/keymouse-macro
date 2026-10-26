# -*- coding: utf-8 -*-
"""生成程序图标 assets/app.ico（渐变圆角底 + 鼠标指针 + 点击高亮点）。"""
import os
from PIL import Image, ImageDraw

OUT_DIR = r"D:\键鼠宏工坊\assets"
os.makedirs(OUT_DIR, exist_ok=True)

S = 256
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

# 竖向渐变底：蓝 → 紫
grad = Image.new("RGB", (S, S))
gd = ImageDraw.Draw(grad)
top = (52, 120, 246)
bottom = (120, 86, 240)
for y in range(S):
    t = y / (S - 1)
    c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
    gd.line([(0, y), (S, y)], fill=c)

# 圆角遮罩
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=56, fill=255)
img.paste(grad, (0, 0), mask)

d = ImageDraw.Draw(img)

# 鼠标指针（白色箭头，带描边）
arrow = [
    (78, 58), (78, 190), (112, 156), (136, 208), (162, 196), (138, 146), (186, 146),
]
d.polygon(arrow, fill=(255, 255, 255, 255))
d.line(arrow + [arrow[0]], fill=(31, 36, 48, 255), width=5, joint="curve")

# 点击高亮点
for r, a in ((30, 60), (22, 110), (14, 255)):
    d.ellipse([196 - r, 76 - r, 196 + r, 76 + r], fill=(255, 92, 92, a))
d.ellipse([196 - 14, 76 - 14, 196 + 14, 76 + 14], fill=(255, 255, 255, 255))
d.ellipse([196 - 8, 76 - 8, 196 + 8, 76 + 8], fill=(232, 74, 74, 255))

path = os.path.join(OUT_DIR, "app.ico")
img.save(path, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("saved " + path + "  " + str(os.path.getsize(path)) + " bytes")
