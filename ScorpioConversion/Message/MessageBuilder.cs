﻿using System;
using System.Collections.Generic;
using System.Text;


public class MessageBuilder
{
    public void Transform(string path)
    {
        Util.InitializeProgram();
        string package = Util.GetConfig(ConfigKey.PackageName, ConfigFile.InitConfig);
        var infos = Util.GetProgramInfos();
        Dictionary<string, List<PackageField>> fields = Util.ParseStructure(path);
        Progress.Count = fields.Count;
        foreach (var pair in fields) {
            ++Progress.Count;
            foreach (var info in infos.Values) {
                if (!info.Create) continue;
                FileUtil.CreateFile(info.GetFile(pair.Key), info.GenerateMessage.Generate(pair.Key, package, pair.Value, false), info.Bom, info.CodeDirectory.Split(';'));
            }
        }
    }
}
