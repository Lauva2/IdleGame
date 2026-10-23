# d18 原创森林树木

`ForestTrees.blend` 含橡树、成年白桦、椴树、疏冠花楸、幼年白桦、低榛六个集合，保留分叉树干、根部、分层树冠和顶点色。来源是本项目自行编写的 `Tools/Make-ForestTrees.py`，没有外部下载模型或付费依赖。forest02未通过后重做轮廓差异、柔和树冠、疏落圆形叶色笔触，清单记录当前网格。

在项目根目录使用已安装 Blender 的独立后台进程重建：

```powershell
& 'G:/SteamLibrary/steamapps/common/Blender/blender.exe' --background --factory-startup --python Tools/Make-ForestTrees.py
```

制作版本为 Blender 5.2.3 LTS。脚本会重新输出本目录 `.blend`、清单及 `Assets/Resources/Models` 中六个 `.bytes`；它不操作另一个正在打开的 Blender 会话。导出使用项目已有 FSTM 格式，Z-up 转 Unity Y-up，包含法线、顶点色与反射后修正的三角形绕序。

修改或重建这些模型会改变构建输入，须重新构建、取证并接受独立美术评审。源码文件和资产许可说明不能替代实际 APK 画面验收。
