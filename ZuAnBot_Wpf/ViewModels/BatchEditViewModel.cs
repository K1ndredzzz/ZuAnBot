using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using ZuAnBot_Wpf.Constants;
using ZuAnBot_Wpf.Helper;

namespace ZuAnBot_Wpf.ViewModels
{
    public class BatchEditViewModel : BindableBase, IDialogAware
    {
        public string Title => "批量编辑";

        public event Action<IDialogResult> RequestClose;

        private string _EditText;
        /// <summary>
        /// 编辑文本内容
        /// </summary>
        public string EditText
        {
            get { return _EditText; }
            set
            {
                SetProperty(ref _EditText, value);
                UpdateWordCount();
            }
        }

        private int _WordCount;
        /// <summary>
        /// 词条数量
        /// </summary>
        public int WordCount
        {
            get { return _WordCount; }
            set { SetProperty(ref _WordCount, value); }
        }

        private WordsCategory _category;

        /// <summary>
        /// 更新词条计数
        /// </summary>
        private void UpdateWordCount()
        {
            if (string.IsNullOrWhiteSpace(EditText))
            {
                WordCount = 0;
            }
            else
            {
                var lines = EditText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(line => !string.IsNullOrWhiteSpace(line))
                                    .ToList();
                WordCount = lines.Count;
            }
        }

        #region 命令

        private DelegateCommand _SaveCommand;
        public DelegateCommand SaveCommand => _SaveCommand ?? (_SaveCommand = new DelegateCommand(ExecuteSaveCommand));

        void ExecuteSaveCommand()
        {
            try
            {
                // 解析文本,每行一个词条
                var lines = EditText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(line => !string.IsNullOrWhiteSpace(line))
                                    .Select(line => line.Trim())
                                    .ToList();

                // 验证每个词条
                foreach (var line in lines)
                {
                    WordsHelper.EnsureValidContent(line);
                }

                // 清空当前词库并添加新词条
                _category.Words.Clear();
                foreach (var line in lines)
                {
                    _category.AddWord(line);
                }

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }

        private DelegateCommand _CancelCommand;
        public DelegateCommand CancelCommand => _CancelCommand ?? (_CancelCommand = new DelegateCommand(ExecuteCancelCommand));

        void ExecuteCancelCommand()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                _category = parameters.GetValue<WordsCategory>(Params.Category);

                // 将现有词条转换为多行文本
                if (_category != null && _category.Words.Count > 0)
                {
                    EditText = string.Join(Environment.NewLine, _category.Words.Select(w => w.Content));
                }
                else
                {
                    EditText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }
    }
}
