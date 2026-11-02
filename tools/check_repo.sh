#!/usr/bin/env bash
# 核验 GitHub 仓库状态（避免在命令行里拼引号）
set -u
REPO="river499/keymouse-macro"

echo "=== 远端 CI 文件 ==="
gh api "repos/$REPO/contents/.github/workflows/build.yml" --jq '.path + "  " + (.size|tostring) + " bytes"' 2>&1 | head -3

echo
echo "=== README 前 4 行 ==="
gh api "repos/$REPO/contents/README.md" --jq '.content' 2>/dev/null | base64 -d | head -4

echo
echo "=== 最近 CI 运行 ==="
gh run list --limit 3 2>&1 | head -5

echo
echo "=== Release 资产 ==="
gh release view v1.0.0 --json assets --jq '.assets[] | .name + "  " + (.size|tostring) + " bytes"'

echo
echo "=== 仓库摘要 ==="
gh repo view --json url,visibility,licenseInfo,stargazerCount --jq '"url=" + .url + "  visibility=" + .visibility + "  license=" + (.licenseInfo.name // "none")'
