# -*- coding: utf-8 -*-
"""收尾：build.cmd 加图标；清理一次性补丁脚本。"""
import io, os, glob

BC = r"D:\键鼠宏工坊\build.cmd"
src = io.open(BC, "r", encoding="utf-8", newline="").read()
if "/win32icon:" in src:
    print("skip: icon already wired")
else:
    old = r"/win32manifest:src\app.manifest src\Program.cs"
    new = r"/win32manifest:src\app.manifest /win32icon:assets\app.ico src\Program.cs"
    if old in src:
        src = src.replace(old, new, 1)
        io.open(BC, "w", encoding="utf-8", newline="").write(src)
        print("ok: icon wired into build")
    else:
        print("FAIL: anchor missing")

for p in glob.glob(r"D:\键鼠宏工坊\tools\patch_*.py"):
    os.remove(p)
    print("removed " + os.path.basename(p))

# .gitignore
gitignore = """build/
dist/*.exe
tests/*.exe
tests/*.log
tests/*.ini
tests/target*.log
profiles/
*.user
*.suo
"""
io.open(r"D:\键鼠宏工坊\.gitignore", "w", encoding="utf-8", newline="\n").write(gitignore)
print("ok: .gitignore")
