using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Prism.Mvvm;
using Prism.Ioc;
using Prism.Events;
using Prism.Commands;
using ZuAnBot_Wpf.Helper;
using Newtonsoft.Json;
using Prism.Services.Dialogs;
using ZuAnBot_Wpf.Views;
using ZuAnBot_Wpf.Constants;
using ZuAnBot_Wpf.Constants.Events;
using System.Windows;

namespace ZuAnBot_Wpf.ViewModels
{
    /// <summary>
    /// 词库，包括多类词条
    /// </summary>
    public class WordsLibrary : BindableBase
    {
        private ObservableCollection<WordsCategory> _Categories;
        public ObservableCollection<WordsCategory> Categories
        {
            get { return _Categories; }
            set { SetProperty(ref _Categories, value); }
        }
    }

    /// <summary>
    /// 词条类
    /// </summary>
    public class WordsCategory : BindableBase
    {
        [JsonIgnore]
        public WordsLibrary Library { get; set; }

        private ObservableCollection<Word> _Words;
        public ObservableCollection<Word> Words
        {
            get { return _Words; }
            set { SetProperty(ref _Words, value); }
        }

        private List<WordsCategory> _TargetCategories;
        [JsonIgnore]
        /// <summary>
        /// 其他类别
        /// </summary>
        public List<WordsCategory> TargetCategories
        {
            get { return _TargetCategories; }
            set { SetProperty(ref _TargetCategories, value); }
        }

        public string CategoryName { get; set; }

        private Word _SelectedWord;
        [JsonIgnore]
        /// <summary>
        /// 当前选中的word
        /// </summary>
        public Word SelectedWord
        {
            get { return _SelectedWord; }
            set
            {
                SetProperty(ref _SelectedWord, value);

                DeleteEnabled = value != null;
                EditEnabled = value != null;
                CopyEnabled = value != null;
            }
        }

        public WordsCategory()
        {
        }

        /// <summary>
        /// 添加词条
        /// </summary>
        public void AddWord(string content)
        {
            var word = new Word { Content = content, Category = this };
            Words.Add(word);
        }

        #region 命令

        public void RefreshSelectedWord()
        {
            SelectedWord = Words.FirstOrDefault(x => x.IsSelected);
        }

        #region AddCommand
        private bool _AddEnabled = true;
        [JsonIgnore]
        public bool AddEnabled
        {
            get { return _AddEnabled; }
            set { SetProperty(ref _AddEnabled, value); }
        }
        private DelegateCommand _AddCommand;
        [JsonIgnore]
        public DelegateCommand AddCommand => _AddCommand ?? (_AddCommand = new DelegateCommand(ExecuteAddCommand).ObservesCanExecute(() => AddEnabled));
        void ExecuteAddCommand()
        {
            try
            {
                AddEnabled = false;

                //弹出词条对话框
                var dialogService = ContainerLocator.Container.Resolve<IDialogService>();

                dialogService.ShowDialog(nameof(WordEdit),
                    new DialogParameters($"{Params.WordContent}={""}"),
                    r =>
                    {
                        if (r.Result == ButtonResult.OK)
                        {
                            var content = r.Parameters.GetValue<string>(Params.WordContent);
                            var word = new Word { Content = content, Category = this };

                            int index;
                            if (SelectedWord == null)
                            {
                                index = 0;
                            }
                            else
                            {
                                index = Words.IndexOf(SelectedWord) + 1;
                            }
                            Words.Insert(index, word);
                        }
                    });

            }
            catch (ArgumentOutOfRangeException e)
            {
                e.Show(showDetail: false);
            }
            catch (Exception e)
            {
                e.Show();
            }
            AddEnabled = true;
        }
        #endregion

        #region DeleteCommand
        private bool _DeleteEnabled = false;
        [JsonIgnore]
        public bool DeleteEnabled
        {
            get { return _DeleteEnabled; }
            set { SetProperty(ref _DeleteEnabled, value); }
        }
        private DelegateCommand _DeleteCommand;
        [JsonIgnore]
        public DelegateCommand DeleteCommand => _DeleteCommand ?? (_DeleteCommand = new DelegateCommand(ExecuteDeleteCommand).ObservesCanExecute(() => DeleteEnabled));
        void ExecuteDeleteCommand()
        {
            try
            {
                DeleteEnabled = false;

                int index = Words.IndexOf(SelectedWord);

                Words.Remove(SelectedWord);

                if (index < Words.Count)
                    Words[index].IsSelected = true;
            }
            catch (Exception e)
            {
                e.Show();
            }
            DeleteEnabled = true;
        }
        #endregion

        #region EditCommand
        private bool _EditEnabled = false;
        [JsonIgnore]
        public bool EditEnabled
        {
            get { return _EditEnabled; }
            set { SetProperty(ref _EditEnabled, value); }
        }
        private DelegateCommand _EditCommand;
        [JsonIgnore]
        public DelegateCommand EditCommand => _EditCommand ?? (_EditCommand = new DelegateCommand(ExecuteEditCommand).ObservesCanExecute(() => EditEnabled));
        void ExecuteEditCommand()
        {
            try
            {
                EditEnabled = false;

                //弹出词条对话框
                var dialogService = ContainerLocator.Container.Resolve<IDialogService>();

                IDialogResult result = null;
                dialogService.ShowDialog(nameof(WordEdit),
                    new DialogParameters($"{Params.WordContent}={SelectedWord.Content}"), r => result = r);

                if (result.Result == ButtonResult.OK)
                {
                    var content = result.Parameters.GetValue<string>(Params.WordContent);

                    SelectedWord.Content = content;
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                e.Show(showDetail: false);
            }
            catch (Exception e)
            {
                e.Show();
            }
            EditEnabled = true;
        }
        #endregion

        #region CopyCommand
        private bool _CopyEnabled = false;
        [JsonIgnore]
        public bool CopyEnabled
        {
            get { return _CopyEnabled; }
            set { SetProperty(ref _CopyEnabled, value); }
        }
        private DelegateCommand _CopyCommand;
        [JsonIgnore]
        public DelegateCommand CopyCommand => _CopyCommand ?? (_CopyCommand = new DelegateCommand(ExecuteCopyCommand).ObservesCanExecute(() => CopyEnabled));
        void ExecuteCopyCommand()
        {
            try
            {
                CopyEnabled = false;

                Clipboard.SetText(SelectedWord.Content, TextDataFormat.Text);
            }
            catch (Exception e)
            {
                e.Show();
            }
            CopyEnabled = true;
        }
        #endregion

        #region CopyToCommand
        private DelegateCommand<string> _CopyToCommand;
        public DelegateCommand<string> CopyToCommand => _CopyToCommand ?? (_CopyToCommand = new DelegateCommand<string>(ExecuteCopyToCommand));
        void ExecuteCopyToCommand(string parameter)
        {
            try
            {
                var sourceWord = Library.Categories.SelectMany(x => x.Words).FirstOrDefault(x => x.IsSelected);

                if (sourceWord == null)
                {
                    MessageHelper.Error("未选中任何词条");
                    return;
                }

                var word = new Word() { Content = sourceWord.Content, Category = this };
                Words.Insert(0, word);
            }
            catch (Exception e)
            {
                e.Show();
            }
        }
        #endregion

        #region PasteCommand
        private bool _PasteEnabled = true;
        [JsonIgnore]
        public bool PasteEnabled
        {
            get { return _PasteEnabled; }
            set { SetProperty(ref _PasteEnabled, value); }
        }
        private DelegateCommand _PasteCommand;

        [JsonIgnore]
        public DelegateCommand PasteCommand => _PasteCommand ?? (_PasteCommand = new DelegateCommand(ExecutePasteCommand).ObservesCanExecute(() => PasteEnabled));
        void ExecutePasteCommand()
        {
            try
            {
                PasteEnabled = false;

                var clipboardText = Clipboard.GetText(TextDataFormat.Text);

                int offset = 1;
                foreach (var text in clipboardText.Split('\n'))
                {
                    if (!WordsHelper.IsVaileContent(text)) continue;

                    var word = new Word { Content = text, Category = this };

                    int index;
                    if (SelectedWord == null)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = Words.IndexOf(SelectedWord) + offset++;
                    }
                    Words.Insert(index, word);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                e.Show(showDetail: false);
            }
            catch (Exception e)
            {
                e.Show();
            }
            PasteEnabled = true;
        }
        #endregion

        #region BatchEditCommand
        private bool _BatchEditEnabled = true;
        [JsonIgnore]
        public bool BatchEditEnabled
        {
            get { return _BatchEditEnabled; }
            set { SetProperty(ref _BatchEditEnabled, value); }
        }
        private DelegateCommand _BatchEditCommand;
        [JsonIgnore]
        public DelegateCommand BatchEditCommand => _BatchEditCommand ?? (_BatchEditCommand = new DelegateCommand(ExecuteBatchEditCommand).ObservesCanExecute(() => BatchEditEnabled));
        void ExecuteBatchEditCommand()
        {
            try
            {
                BatchEditEnabled = false;

                var dialogService = ContainerLocator.Container.Resolve<IDialogService>();
                var parameters = new DialogParameters();
                parameters.Add(Params.Category, this);

                dialogService.ShowDialog(nameof(BatchEdit), parameters, r =>
                {
                    // 操作已在 BatchEditViewModel 中完成
                });
            }
            catch (Exception e)
            {
                e.Show();
            }
            BatchEditEnabled = true;
        }
        #endregion

        #region BatchImportCommand
        private bool _BatchImportEnabled = true;
        [JsonIgnore]
        public bool BatchImportEnabled
        {
            get { return _BatchImportEnabled; }
            set { SetProperty(ref _BatchImportEnabled, value); }
        }
        private DelegateCommand _BatchImportCommand;
        [JsonIgnore]
        public DelegateCommand BatchImportCommand => _BatchImportCommand ?? (_BatchImportCommand = new DelegateCommand(ExecuteBatchImportCommand).ObservesCanExecute(() => BatchImportEnabled));
        void ExecuteBatchImportCommand()
        {
            try
            {
                BatchImportEnabled = false;

                // 打开文件选择对话框
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择文本文件",
                    Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var fileContent = System.IO.File.ReadAllText(openFileDialog.FileName);

                    // 解析文本,每行一个词条
                    var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Where(line => !string.IsNullOrWhiteSpace(line))
                                           .Select(line => line.Trim())
                                           .ToList();

                    if (lines.Count == 0)
                    {
                        MessageHelper.Warning("文件中没有有效的词条");
                        return;
                    }

                    // 验证每个词条
                    foreach (var line in lines)
                    {
                        WordsHelper.EnsureValidContent(line);
                    }

                    // 询问是否追加还是替换
                    var result = MessageBox.Show(
                        $"找到 {lines.Count} 条词句\n\n点击【是】追加到当前词库\n点击【否】替换当前词库\n点击【取消】取消导入",
                        "批量导入",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    if (result == MessageBoxResult.No)
                    {
                        // 替换模式：清空现有词条
                        Words.Clear();
                    }

                    // 添加新词条
                    foreach (var line in lines)
                    {
                        AddWord(line);
                    }

                    MessageHelper.Info($"成功导入 {lines.Count} 条词句");
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                e.Show(showDetail: false);
            }
            catch (Exception e)
            {
                e.Show();
            }
            BatchImportEnabled = true;
        }
        #endregion

        #endregion 命令

    }

    /// <summary>
    /// 词条
    /// </summary>
    public class Word : BindableBase
    {
        [JsonIgnore]
        public WordsCategory Category { get; set; }

        private string _Content;
        /// <summary>
        /// 内容
        /// </summary>
        public string Content
        {
            get { return _Content; }
            set
            {
                value = value.Replace('\r', char.MinValue);

                WordsHelper.EnsureValidContent(value);

                SetProperty(ref _Content, value);
            }
        }

        private bool _IsSelected;
        [JsonIgnore]
        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                SetProperty(ref _IsSelected, value);

                Category.RefreshSelectedWord();
            }
        }
    }

    public static class WordsLibraryExtension
    {
        public static string GetLoacalWord(this WordsLibrary library, string categoryName)
        {
            var words = library.Categories.FirstOrDefault(x => x.CategoryName == categoryName);

            if (words == null || words.Words.Count == 0)
            {
                throw new InvalidOperationException($"词库 \"{categoryName}\" 为空，请先添加词条");
            }

            Random random = new Random((int)DateTime.Now.Ticks);
            var word = words.Words[random.Next(0, words.Words.Count)];

            return word.Content;
        }

        //static int i = 0;
        //public static string GetTestWord(out int index)
        //{
        //    index = i;
        //    if (i >= wordsLibrary.Categories.Count) return "测试完毕！";

        //    WordsLibrary words = wordsLibrary;

        //    var word = words.Categories[i++];

        //    return word;
        //}
    }
}
