using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using WindowsInput;
using ZuAnBot_Wpf.Api;
using ZuAnBot_Wpf.Constants;
using ZuAnBot_Wpf.Helper;
using ZuAnBot_Wpf.Views;

namespace ZuAnBot_Wpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IDialogService _dialogService;
        private readonly Apis _apis = Apis.GetInstance();
        GlobalKeyboardHook hook;

        public WordsLibrary Library { get; set; }

        #region 绑定属性
        private bool _IsPerWord = false;
        /// <summary>
        /// 是否逐字发送
        /// </summary>
        public bool IsPerWord
        {
            get { return _IsPerWord; }
            set { SetProperty(ref _IsPerWord, value); }
        }

        private bool _IsAll = false;
        /// <summary>
        /// 是否发送所有人消息
        /// </summary>
        public bool IsAll
        {
            get { return _IsAll; }
            set { SetProperty(ref _IsAll, value); }
        }


        private bool _IsNotifyIconBlink;
        /// <summary>
        /// 托盘图标是否闪烁
        /// </summary>
        public bool IsNotifyIconBlink
        {
            get { return _IsNotifyIconBlink; }
            set { SetProperty(ref _IsNotifyIconBlink, value); }
        }

        private bool _IsNotifyIconShow = true;
        /// <summary>
        /// 托盘图标是否显示
        /// </summary>
        public bool IsNotifyIconShow
        {
            get { return _IsNotifyIconShow; }
            set { SetProperty(ref _IsNotifyIconShow, value); }
        }

        private string _Version;
        /// <summary>
        /// 程序版本号
        /// </summary>
        public string Version
        {
            get { return _Version; }
            set { SetProperty(ref _Version, value); }
        }

        private bool _NeedUpdate;
        /// <summary>
        /// 是否需要更新
        /// </summary>
        public bool NeedUpdate
        {
            get { return _NeedUpdate; }
            set { SetProperty(ref _NeedUpdate, value); }
        }

        private bool _EnableObfuscation = false;
        /// <summary>
        /// 是否启用文字混淆（拼音替换）
        /// </summary>
        public bool EnableObfuscation
        {
            get { return _EnableObfuscation; }
            set { SetProperty(ref _EnableObfuscation, value); }
        }

        #endregion 绑定属性

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        #region 命令
        #region LoadedCommand
        private DelegateCommand _LoadedCommand;
        public DelegateCommand LoadedCommand => _LoadedCommand ?? (_LoadedCommand = new DelegateCommand(ExecuteLoadedCommand));
        async void ExecuteLoadedCommand()
        {
            try
            {
                LoadWordsLibrary();

                HookKeys();

                Version = "v" + System.Windows.Application.ResourceAssembly.GetName().Version.ToString(3);

                await InitNeedUpdate();
            }
            catch (Exception e)
            {
                e.Show();
            }
        }

        private async Task InitNeedUpdate()
        {
            var latestVersion = await VersionHelper.GetLatestVersion();
            NeedUpdate = !VersionHelper.IsNewestVersion(latestVersion);
        }

        private void LoadWordsLibrary()
        {
            try
            {
                //第一试用本软件没有本地词库，用资源清单的词库
                if (!File.Exists(LocalConfigHelper.WordsLibraryPath))
                {
                    var dir = Path.GetDirectoryName(LocalConfigHelper.WordsLibraryPath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var manifestStream = ManifestHelper.GetManifestStream("wordsLibrary.json");
                    using (var stream = File.Create(LocalConfigHelper.WordsLibraryPath))
                    {
                        manifestStream.CopyTo(stream);
                    }
                }

                Library = JsonHelper.DeserializeWordsLibrary();//反序列化本地词库

                // 升级旧版词库:将"默认词库"/"自定义词库"迁移到"词库1-5"
                UpgradeOldLibrary();
            }
            catch (Exception e)
            {
                e.Show("读取词库失败!");
                File.Delete(LocalConfigHelper.WordsLibraryPath);
                App.Current.Shutdown();
            }
        }

        /// <summary>
        /// 升级旧版词库结构
        /// </summary>
        private void UpgradeOldLibrary()
        {
            bool needUpgrade = false;

            // 检查是否有旧版词库名称
            var defaultLib = Library.Categories.FirstOrDefault(x => x.CategoryName == "默认词库");
            var customLib = Library.Categories.FirstOrDefault(x => x.CategoryName == "自定义词库");

            if (defaultLib != null)
            {
                defaultLib.CategoryName = "词库1";
                needUpgrade = true;
            }

            if (customLib != null)
            {
                customLib.CategoryName = "词库2";
                needUpgrade = true;
            }

            // 确保有5个词库
            var existingNames = Library.Categories.Select(x => x.CategoryName).ToList();
            for (int i = 1; i <= 5; i++)
            {
                string categoryName = $"词库{i}";
                if (!existingNames.Contains(categoryName))
                {
                    Library.Categories.Add(new WordsCategory
                    {
                        CategoryName = categoryName,
                        Words = new System.Collections.ObjectModel.ObservableCollection<Word>(),
                        Library = Library
                    });
                    needUpgrade = true;
                }
            }

            // 如果有升级,保存到本地
            if (needUpgrade)
            {
                JsonHelper.SerializeWordsLibrary(Library);
            }
        }

        #endregion

        #region 访问GitHub
        private DelegateCommand _VisitGitHubCommand;
        public DelegateCommand VisitGitHubCommand => _VisitGitHubCommand ?? (_VisitGitHubCommand = new DelegateCommand(ExecuteVisitGitHubCommand));
        void ExecuteVisitGitHubCommand()
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/liuke-wuhan/ZuAnBot");
            }
            catch (Exception e)
            {
                e.Show();
            }
        }
        #endregion

        #region SetCommand
        private DelegateCommand _SetCommand;

        public DelegateCommand SetCommand => _SetCommand ?? (_SetCommand = new DelegateCommand(ExecuteSetCommand));
        void ExecuteSetCommand()
        {
            try
            {
                var parameters = new DialogParameters();
                parameters.Add(Params.Library, Library);
                IDialogResult r = null;
                _dialogService.ShowDialog(nameof(WordsLibrarySet), parameters, result => r = result);

                if (r.Result == ButtonResult.OK)
                {
                    JsonHelper.SerializeWordsLibrary(Library);
                }
                else
                {
                    LoadWordsLibrary();
                }
            }
            catch (Exception e)
            {
                e.Show();
            }
        }
        #endregion

        #region UpdateCommand
        private bool _UpdateEnabled = true;
        public bool UpdateEnabled
        {
            get { return _UpdateEnabled; }
            set { SetProperty(ref _UpdateEnabled, value); }
        }
        private DelegateCommand _UpdateCommand;
        public DelegateCommand UpdateCommand => _UpdateCommand ?? (_UpdateCommand = new DelegateCommand(ExecuteUpdateCommand).ObservesCanExecute(() => UpdateEnabled));
        async void ExecuteUpdateCommand()
        {
            try
            {
                UpdateEnabled = false;

                var latestVersion = await VersionHelper.GetLatestVersion();
                if (VersionHelper.IsNewestVersion(latestVersion))
                {
                    MessageHelper.Info($"当前版本已经是最新版本");
                }
                else
                {
                    var result = MessageHelper.Question($"当前版本为{VersionHelper.GetCurrentVersionName()}，最新版本为{latestVersion.VersionName}，是否更新？");
                    if (result)
                    {
                        var tempDir = Path.GetTempPath();//临时目录
                        var file = _apis.DownloadFile(tempDir, latestVersion.FileName, latestVersion.Url);//下载到临时目录

                        //使用更新
                        var assembly = Assembly.GetExecutingAssembly();
                        var stream = assembly.GetManifestResourceStream("costura.zuanbotupdate.exe");
                        var tempPath = Path.Combine(Path.GetTempPath(), "zuanbotupdate.exe");
                        using (var tempStream = File.Create(tempPath))
                        {
                            stream.CopyTo(tempStream);
                        }

                        var startInfo = new ProcessStartInfo(tempPath, $"\"{file}\" \"{assembly.Location}\"");
                        //设置不在新窗口中启动新的进程
                        startInfo.CreateNoWindow = true;
                        //不使用操作系统使用的shell启动进程
                        startInfo.UseShellExecute = false;
                        //将输出信息重定向
                        startInfo.RedirectStandardOutput = true;
                        Process.Start(startInfo);
                    }
                }
            }
            catch (Exception e)
            {
                e.Show();
            }
            finally
            {
                UpdateEnabled = true;
            }
        }
        #endregion


        #endregion

        /// <summary>
        /// 按键勾子
        /// </summary>
        private void HookKeys()
        {
            hook = new GlobalKeyboardHook();
            hook.KeyUp += Hook_KeyUp;
            hook.HookedKeys.Add(Keys.F2);  // 词库1
            hook.HookedKeys.Add(Keys.F3);  // 词库2
            hook.HookedKeys.Add(Keys.F4);  // 词库3
            hook.HookedKeys.Add(Keys.F5);  // 词库4
            hook.HookedKeys.Add(Keys.F6);  // 词库5
            hook.HookedKeys.Add(Keys.F11); // 切换全员发送
            hook.HookedKeys.Add(Keys.F12); // 切换逐字发送
            hook.hook();
        }

        /// <summary>
        /// 勾子事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hook_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                string word = "";
                if (e.KeyCode == Keys.F2)
                    word += Library.GetLoacalWord("词库1");
                else if (e.KeyCode == Keys.F3)
                    word += Library.GetLoacalWord("词库2");
                else if (e.KeyCode == Keys.F4)
                    word += Library.GetLoacalWord("词库3");
                else if (e.KeyCode == Keys.F5)
                    word += Library.GetLoacalWord("词库4");
                else if (e.KeyCode == Keys.F6)
                    word += Library.GetLoacalWord("词库5");
                else if (e.KeyCode == Keys.F11)
                {
                    IsAll = !IsAll;
                    return;
                }
                else if (e.KeyCode == Keys.F12)
                {
                    IsPerWord = !IsPerWord;
                    return;
                }
                else
                {
                    return;
                }

                // 应用文字混淆
                if (EnableObfuscation)
                {
                    word = TextObfuscator.Obfuscate(word);
                }

                string allPre = IsAll ? "/all " : "";

                var builder = Simulate.Events();
                if (IsPerWord)
                {
                    // 逐字发送模式
                    if (EnableObfuscation && word.Contains(" "))
                    {
                        // 混淆模式: 按空格分词发送(每个拼音单独发送)
                        var parts = word.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            builder = builder.
                                Click(WindowsInput.Events.KeyCode.Enter).Wait(100).
                                Click(allPre + part).Wait(100).
                                Click(WindowsInput.Events.KeyCode.Enter).Wait(100);
                        }
                    }
                    else
                    {
                        // 普通模式: 逐字符发送
                        foreach (var item in word)
                        {
                            builder = builder.
                                Click(WindowsInput.Events.KeyCode.Enter).Wait(100).
                                Click(allPre + item).Wait(100).
                                Click(WindowsInput.Events.KeyCode.Enter).Wait(100);
                        }
                    }
                }
                else
                {
                    builder = builder.
                        Click(WindowsInput.Events.KeyCode.Enter).Wait(100).
                        Click(allPre + word).Wait(100).
                        Click(WindowsInput.Events.KeyCode.Enter).Wait(100);
                }
                builder.Invoke();
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageHelper.Error($"词库为空");
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }

    }
}
