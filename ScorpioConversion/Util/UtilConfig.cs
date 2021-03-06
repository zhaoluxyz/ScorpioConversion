﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using ScorpioConversion;

public class DefaultInfo : Attribute
{
    public string Extension;        //文件后缀
    public Type GenerateTable;      //转换Table类
    public Type GenerateMessage;    //转换Message类
    public Type GenerateEnum;       //转换Enum类
    public bool Bom;                //文件是否有bom头
    public DefaultInfo(string ex, Type table, Type message, Type @enum, bool bom)
    {
        Extension = ex;
        GenerateTable = table;
        GenerateMessage = message;
        GenerateEnum = @enum;
        Bom = bom;
    }
}
//语言数量
public enum PROGRAM
{
    NONE = -1,      //无语言
    [DefaultInfo("cs", typeof(GenerateTableCSharp), typeof(GenerateMessageCSharp), typeof(GenerateEnumCSharp), true)]
    CSharp,         //C#(CSharp) 语言
    [DefaultInfo("java", typeof(GenerateTableJava), typeof(GenerateMessageJava), typeof(GenerateEnumJava), false)]
    Java,           //Java 语言
    [DefaultInfo("js", typeof(GenerateTableScorpio), typeof(GenerateMessageScorpio), typeof(GenerateEnumScorpio), false)]
    Scorpio,        //Scorpio 脚本
    //[DefaultInfo("h", null)]
    //Cpp,
    COUNT,
}
//所有配置文件
public enum ConfigFile
{
    PathConfig,
    InitConfig,
    TableConfig,
    LanguageConfig,
    CunstomConfig,
}
//一种语言的信息
public class ProgramInfo
{
    private static readonly Type TYPE_TABLE_BUILDER = typeof(TableBuilder);
    private static readonly Type TYPE_MESSAGE_BUILDER = typeof(MessageBuilder);
    public PROGRAM Code;                //
    public bool Create;                 //默认是否生成
    public string CodeDirectory;        //代码输出目录
    public string DataDirectory;        //文件输出目录
    public bool Compress;               //data文件是否压缩
    public string Extension;            //扩展名
    public IGenerate GenerateTable;     //Table生成代码类
    public IGenerate GenerateMessage;   //Message生成代码类
    public IGenerate GenerateEnum;      //Enum生成代码类
    public bool Bom;                    //是否有bom文件头
    public MethodInfo CreateTableManager;    //生成TableManager文件
    public MethodInfo CreateMessageManager;    //生成TableManager文件
    public ProgramInfo(PROGRAM code)
    {
        Code = code;
        CreateTableManager = TYPE_TABLE_BUILDER.GetMethod("CreateManager" + code.ToString());
        CreateMessageManager = TYPE_MESSAGE_BUILDER.GetMethod("CreateManager" + code.ToString());
    }
    public string GetFile(string filter)
    {
        return filter + "." + Extension;
    }
    public string TableTemplate { 
        get {
            string file = Util.CurrentDirectory + "/Template/Table." + Extension;
            return FileUtil.FileExist(file) ? FileUtil.GetFileString(file) : "";
        }
    }
    public string HeadTemplate {
        get {
            string file = Util.CurrentDirectory + "/Template/Head." + Extension;
            return FileUtil.FileExist(file) ? FileUtil.GetFileString(file) : "";
        }
    }
    public void CreateFile(string name, string context)
    {
        FileUtil.CreateFile(GetFile(name), context, Bom, CodeDirectory.Split(';'));
    }
    public ProgramInfo Clone()
    {
        ProgramInfo ret = new ProgramInfo(Code);
        ret.Create = Create;
        ret.CodeDirectory = CodeDirectory;
        ret.DataDirectory = DataDirectory;
        ret.Compress = Compress;
        ret.Extension = Extension;
        ret.GenerateTable = GenerateTable;
        ret.GenerateMessage = GenerateMessage;
        ret.GenerateEnum = GenerateEnum;
        ret.CreateTableManager = CreateTableManager;
        ret.CreateMessageManager = CreateMessageManager;
        return ret;
    }
}
public class ConfigKey
{
    //PathConfig
    public const string CodeDirectory = "CodeDirectory";            //CS代码生成目录
    public const string DataDirectory = "DataDirectory";            //CS数据文件生成目录
    public const string TransformDirectory = "TransformDirectory";  //转换文件目录
    public const string RollbackDirectory = "RollbackDirectory";    //反转文件目录
    //InitConfig
    public const string SpawnList = "SpawnList";                    //批量处理关键字
    public const string Create = "Create";                          //是否默认生成
    public const string Compress = "Compress";                      //data文件是否使用
    public const string Languages = "Languages";                    //所有多国语言
    public const string PackageName = "PackageName";                //PackageName
    public const string MessagePath = "MessagePath";                //MessagePath
    //TableConfig
    public const string Array = "Array";                            //是否是数组形式
    //LanguageConfig
    public const string AllLanguage = "AllLanguage";
    public const string TranslationDirectory = "TranslationDirectory";
    public const string LanguageDirectory = "LanguageDirectory";
}
public static partial class Util
{
    private class AutoConfig
    {
        public PROGRAM program;
        public string key;
        public ConfigFile file;
    }
    private static Dictionary<ConfigFile, Config> m_Configs = new Dictionary<ConfigFile, Config>();
    private static Dictionary<PROGRAM, ProgramInfo> m_ProgramInfos = new Dictionary<PROGRAM, ProgramInfo>();
    private static Dictionary<object, AutoConfig> m_AutoConfigs = new Dictionary<object, AutoConfig>();
    public static void Bind(TextBox textBox, string key, ConfigFile file)
    {
        Bind(textBox, PROGRAM.NONE, key, file);
    }
    public static void Bind(TextBox textBox, PROGRAM program, string key, ConfigFile file)
    {
        if (m_AutoConfigs.ContainsKey(textBox))
            return;
        AutoConfig config = new AutoConfig() { program = program, key = key, file = file };
        textBox.Text = GetConfig(program, key, file);
        textBox.TextChanged += new System.EventHandler(TextChanged);
        m_AutoConfigs[textBox] = config;
    }
    private static void TextChanged(object sender, EventArgs e)
    {
        if (m_AutoConfigs.ContainsKey(sender))
        {
            AutoConfig config = m_AutoConfigs[sender];
            SetConfig(config.program, config.key, ((TextBox)sender).Text, config.file);
        }
    }
    public static bool ToBoolean(string str)
    {
        return ToBoolean(str, false);
    }
    public static bool ToBoolean(string str, bool def)
    {
        if (string.IsNullOrEmpty(str)) return def;
        switch (str.ToLower())
        {
            case "1":
            case "true":
                return true;
            case "0":
            case "false":
                return false;
            default:
                throw new Exception("字符串不能转换为bool " + str);
        }
    }
    private static Config GetConfig(ConfigFile file)
    {
        Config config = null;
        if (!m_Configs.ContainsKey(file)) {
            config = new Config(CurrentDirectory + file.ToString() + ".ini", true);
            m_Configs.Add(file, config);
        } else {
            config = m_Configs[file];
        }
        return config;
    }
    public static string GetConfig(string key, ConfigFile file)
    {
        return GetConfig(PROGRAM.NONE, key, file);
    }
    public static void SetConfig(string key, string value, ConfigFile file)
    {
        SetConfig(PROGRAM.NONE, key, value, file);
    }
    public static string GetConfig(PROGRAM program, string key, ConfigFile file)
    {
        Config config = GetConfig(file);
        return config.Get(program == PROGRAM.NONE ? "" : program.ToString(), key);
    }
    public static void SetConfig(PROGRAM program, string key, string value, ConfigFile file)
    {
        Config config = GetConfig(file);
        config.Set(program == PROGRAM.NONE ? "" : program.ToString(), key, value);
        config.Save(false);
    }
    public static string GetConfig(string section, string key, ConfigFile file)
    {
        Config config = GetConfig(file);
        return config.Get(section, key);
    }
    public static void InitializeProgram()
    {
        m_ProgramInfos.Clear();
        for (int i = (int)PROGRAM.NONE + 1; i < (int)PROGRAM.COUNT;++i )
        {
            PROGRAM program = (PROGRAM)i;
            ProgramInfo info = new ProgramInfo(program);
            string CodeDirectory = GetConfig(program, ConfigKey.CodeDirectory, ConfigFile.PathConfig);
            string DataDirectory = GetConfig(program, ConfigKey.DataDirectory, ConfigFile.PathConfig);
            info.CodeDirectory = string.IsNullOrEmpty(CodeDirectory) ? CurrentDirectory + program.ToString() : CodeDirectory;
            info.DataDirectory = string.IsNullOrEmpty(DataDirectory) ? CurrentDirectory + program.ToString() : DataDirectory;
            info.Create = ToBoolean(GetConfig(program, ConfigKey.Create, ConfigFile.PathConfig), true);
            info.Compress = ToBoolean(GetConfig(program, ConfigKey.Compress, ConfigFile.InitConfig), false);
            DefaultInfo defaultInfo = (DefaultInfo)Attribute.GetCustomAttribute(program.GetType().GetMember(program.ToString())[0], typeof(DefaultInfo));
            info.Extension = defaultInfo.Extension;
            info.GenerateTable = (IGenerate)System.Activator.CreateInstance(defaultInfo.GenerateTable);
            info.GenerateMessage = (IGenerate)System.Activator.CreateInstance(defaultInfo.GenerateMessage);
            info.GenerateEnum = (IGenerate)System.Activator.CreateInstance(defaultInfo.GenerateEnum);
            info.Bom = defaultInfo.Bom;
            m_ProgramInfos.Add(program, info);
        }
    }
    public static ProgramInfo GetProgramInfo(PROGRAM program)
    {
        if (m_ProgramInfos.ContainsKey(program))
            return m_ProgramInfos[program];
        return null;
    }
    public static Dictionary<PROGRAM, ProgramInfo> GetProgramInfos()
    {
        return m_ProgramInfos;
    }
    public static void SetToolTip(Control control, string text)
    {
        var tips = new ToolTip();
        tips.ShowAlways = true;
        tips.InitialDelay = 1;
        tips.IsBalloon = true;
        tips.SetToolTip(control, text);
    }
}

