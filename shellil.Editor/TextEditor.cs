using shellil.Editor.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shellil.Editor
{
    public class TextEditor
    {
        public int TabCount => _Tabs.Count;
        public TextEditorTab ActiveTab
        {
            get => _ActiveTab;
            set
            {
                lock (_Sync)
                {
                    if (!_Tabs.Contains(value))
                        throw new ArgumentException($"{nameof(value)} is not a tab in this editor");
                    _ActiveTab = value;
                }
            }
        }

        public IUserPrompt? UserPrompt
        {
            get => _UserPrompt;
            set
            {
                lock (_Sync)
                    _UserPrompt = value;
            }
        }

        public IAppearance Appearance { get; }

        public TextEditor(IAppearance appearance)
        {
            _ActiveTab = new TextEditorTab();
            _Tabs.Add(_ActiveTab);
            Appearance = appearance;
        }

        private List<TextEditorTab> _Tabs = new List<TextEditorTab>();
        private TextEditorTab _ActiveTab;
        private IUserPrompt? _UserPrompt;
        private object _Sync = new object();

        public void SetUserPrompt()
        {

        }

        public TextEditorTab NewTab()
        {
            var newTab = new TextEditorTab();
            lock (_Sync)
                _Tabs.Add(newTab);
            return newTab;
        }

        public TextEditorTab NewTab(string filePath)
        {
            var newTab = new TextEditorTab();
            newTab.Open(filePath);
            lock (_Sync)
                _Tabs.Add(newTab);
            return newTab;
        }

        public void CloseTab(TextEditorTab tab)
        {
            lock (_Sync)
                _Tabs.Remove(tab);
        }

        public TextEditorTab GetTab(int tabIndex) => _Tabs[tabIndex];
    }
}
