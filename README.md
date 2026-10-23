# IdleGame — 林间奇物铺

安卓竖屏、单机离线的 3D 治愈放置经营游戏。玩家经营森林中的树洞魔法小店，通过种植原料、加工商品、自动销售和居民委托，逐步修复并扩建店铺。

## 当前阶段

已按用户要求改为实际3D，当前产出d11-release01非开发安卓候选APK。普通真实第一章、茶会继续及四槽装饰重启持久化已验证；MuMu本包同进程测试30分44秒；独立最终美术91.5/100通过。声音人工听感尚待确认，完整第一版尚未全部通过。APK、哈希、实际测试与剩余门槛见[交付说明](Docs/交付说明.md)。

- 画风：暖色绘本感 3D 卡通。
- 语言：简体中文。
- 首版：免费、无广告、无内购。
- 内容目标：四种作物、两台设备、六种商品、五位熟客及完整第一章。

## 设计与执行规定

- [游戏设计文档 v0.1](Docs/林间奇物铺-游戏设计文档-v0.1.md)
- [第一版成品开发工作流](Docs/第一版成品开发工作流.md)
- [代理执行规定](AGENTS.md)

用户已明确启动开发及 3D 改造，并于2026-10-23明确改为MuMu最终设备验收、接受实体手机未验证限制。必须由独立美术评审子代理验收最终3D候选，美术达到90/100、单项达标且无阻断问题，同时通过功能、存档、离线经营、声音和修订后的安卓验收，才能宣布第一版成品完成。历史2D结果不等于3D通过。

- [开发任务清单](Docs/开发任务清单.md)
- [验收矩阵](Docs/验收矩阵.md)
- [3D 美术规范](Docs/美术规范.md)

纯核心验证运行 `Tools/Test-Core.ps1`。Unity 菜单 `Forest Shop/Prepare Project` 准备入口场景，`Forest Shop/Build Windows Test Player` 和 `Forest Shop/Build Android Test APK` 产出开发测试包，`Forest Shop/Build Android Release APK` 产出非开发IL2CPP双架构包。匹配7670c08855a9的Android Build Support已经补齐；d11-release01已实际ARM64新装和更新启动，核心22项测试通过。历史隔离旧编辑器兼容包不能当作最终交付。

## 打开工程

1. 使用 Unity Hub 安装 **Unity 2022.3.62f2**；后续安卓开发需安装对应的 Android Build Support、SDK、NDK 和 OpenJDK 模块。
2. 在 Unity Hub 中添加并打开仓库根目录。
3. 等待 Unity 恢复包依赖并导入资源。

版本控制保留 `Assets`（含 `.meta`）、`Packages` 和 `ProjectSettings`。`Library`、日志、本机设置、IDE 生成文件和构建产物由 `.gitignore` 排除，Unity 会在本地重新生成所需缓存。
