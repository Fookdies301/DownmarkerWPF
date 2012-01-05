﻿using System.IO;
using System.Text.RegularExpressions;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Document;
using MarkdownSharp;
using MarkPad.Services.Interfaces;

namespace MarkPad.Document
{
    internal class DocumentViewModel : Screen
    {
        private readonly IDialogService dialogService;

        private string title;
        private string filename;

        public DocumentViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            title = "New Document";
            Original = "";
            Document = new TextDocument();
        }

        public void Open(string path)
        {
            filename = path;
            title = new FileInfo(path).Name;

            var text = File.ReadAllText(path);
            Document.Text = text;
            Original = text;
        }

        public void Update()
        {
            NotifyOfPropertyChange(() => Render);
            NotifyOfPropertyChange(() => HasChanges);
            NotifyOfPropertyChange(() => DisplayName);
        }

        public bool Save()
        {
            if (!HasChanges)
                return true;

            if (string.IsNullOrEmpty(filename))
            {
                var path = dialogService.GetFileSavePath("Choose a location to save the document.", "*.md", "Markdown Files (*.md)|*.md|All Files (*.*)|*.*");

                if (string.IsNullOrEmpty(path))
                    return false;

                filename = path;
                title = new FileInfo(filename).Name;
            }

            File.WriteAllText(filename, Document.Text);
            Original = Document.Text;

            return true;
        }

        public TextDocument Document { get; set; }
        public string Original { get; set; }

        public string Render
        {
            get
            {
                var markdown = new Markdown();

                var textToRender = StripHeader(Document.Text);

                return markdown.Transform(textToRender);
            }
        }

        private static string StripHeader(string text)
        {
            const string delimiter = "---";
            var matches = Regex.Matches(text, delimiter, RegexOptions.Multiline);

            if (matches.Count != 2)
            {
                return text;
            }

            var startIndex = matches[0].Index;
            var endIndex = matches[1].Index;
            var length = endIndex - startIndex + delimiter.Length;
            var textToReplace = text.Substring(startIndex, length);

            return text.Replace(textToReplace, string.Empty);
        }

        public bool HasChanges
        {
            get { return Original != Document.Text; }
        }

        public override string DisplayName
        {
            get { return title + (HasChanges ? " *" : ""); }
        }

        public override void CanClose(System.Action<bool> callback)
        {
            if (!HasChanges)
            {
                callback(true);
                return;
            }

            var saveResult = dialogService.ShowConfirmationWithCancel("MarkPad", "Do you want to save changes to `" + title + "`?", "");
            bool result = false;

            // true = Yes
            if (saveResult == true)
            {
                result = Save();
            }
            // false = No
            else if (saveResult == false)
            {
                result = true;
            }
            // no result = Cancel
            else if (!saveResult.HasValue)
            {
                result = false;
            }

            callback(result);
        }
    }
}