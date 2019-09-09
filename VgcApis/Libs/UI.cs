﻿using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using VgcApis.Resources.Langs;

namespace VgcApis.Libs
{
    public static class UI
    {
        #region Controls
        public static bool TryParseAddressFromTextBox(TextBox tbox, out string ip, out int port)
        {
            if (Libs.Utils.TryParseIPAddr(tbox.Text, out ip, out port))
            {
                if (tbox.ForeColor != Color.Black)
                {
                    tbox.ForeColor = Color.Black;
                }
                return true;
            }

            // UI operation is expansive
            if (tbox.ForeColor != Color.Red)
            {
                tbox.ForeColor = Color.Red;
            }
            return false;
        }
        #endregion

        #region update ui
        /// <summary>
        /// If control==null return;
        /// </summary>
        /// <param name="control">invokeable control</param>
        /// <param name="updateUi">UI updater</param>
        public static void RunInUiThread(Control control, Action updateUi)
        {
            if (control == null || control.IsDisposed)
            {
                return;
            }

            if (control.InvokeRequired)
            {
                control.Invoke((MethodInvoker)delegate
                {
                    updateUi();
                });
            }
            else
            {
                updateUi();
            }
        }

        // https://stackoverflow.com/questions/87795/how-to-prevent-flickering-in-listview-when-updating-a-single-listviewitems-text
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }

        public static void ScrollToBottom(RichTextBox control)
        {
            control.SelectionStart = control.Text.Length;
            control.ScrollToCaret();
        }
        #endregion

        #region file
        static public string ReadFileContentFromDialog(string extension)
        {
            var tuple = ReadFileFromDialog(extension);
            return tuple.Item1;
        }

        /// <summary>
        /// <para>return(content, filename)</para>
        /// <para>[content] Null: cancelled String.Empty: file is empty or read fail.</para>
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public Tuple<string, string> ReadFileFromDialog(string extension)
        {
            OpenFileDialog readFileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = extension,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true,
                ShowHelp = true,
            };

            var fileName = string.Empty;

            if (readFileDialog.ShowDialog() != DialogResult.OK)
            {
                return new Tuple<string, string>(null, fileName);
            }

            fileName = readFileDialog.FileName;
            var content = string.Empty;
            try
            {
                content = File.ReadAllText(fileName);
            }
            catch { }
            return new Tuple<string, string>(content, fileName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="extentions"></param>
        /// <returns>file name</returns>
        static public string SaveToFile(string extentions, string content)
        {
            var err = ShowSaveFileDialog(
                    extentions,
                    content,
                    out string filename);

            switch (err)
            {
                case Models.Datas.Enum.SaveFileErrorCode.Success:
                    MessageBox.Show(I18N.Done);
                    break;
                case Models.Datas.Enum.SaveFileErrorCode.Fail:
                    MessageBox.Show(I18N.WriteFileFail);
                    break;
                case Models.Datas.Enum.SaveFileErrorCode.Cancel:
                    // do nothing
                    break;
            }
            return filename;
        }

        public static Models.Datas.Enum.SaveFileErrorCode ShowSaveFileDialog(
            string extension, string content, out string fileName)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = extension,
                RestoreDirectory = true,
                Title = I18N.SaveAs,
                ShowHelp = true,
            };

            saveFileDialog.ShowDialog();

            fileName = saveFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                return Models.Datas.Enum.SaveFileErrorCode.Cancel;
            }

            try
            {
                File.WriteAllText(fileName, content);
                return Models.Datas.Enum.SaveFileErrorCode.Success;
            }
            catch { }
            return Models.Datas.Enum.SaveFileErrorCode.Fail;
        }

        /// <summary>
        /// Return file name.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string ShowSelectFileDialog(string extension)
        {
            OpenFileDialog readFileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = extension,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true,
                ShowHelp = true,
            };

            var fileName = string.Empty;

            if (readFileDialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return readFileDialog.FileName;
        }

        #endregion

        #region popup
        public static void VisitUrl(string msg, string url)
        {
            var text = string.Format("{0}\n{1}", msg, url);
            if (Confirm(text))
            {
                Utils.RunInBackground(() => System.Diagnostics.Process.Start(url));
            }
        }

        public static void MsgBox(string content) =>
            MsgBox("", content);

        public static void MsgBox(string title, string content) =>
            MessageBox.Show(content ?? string.Empty, title ?? string.Empty);

        public static void MsgBoxAsync(string content) =>
            Utils.RunInBackground(() => MsgBox("", content));

        public static void MsgBoxAsync(string title, string content) =>
            Utils.RunInBackground(() => MsgBox(title, content));

        public static bool Confirm(string content)
        {
            var confirm = MessageBox.Show(
                content,
                I18N.Confirm,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            return confirm == DialogResult.Yes;
        }

        #endregion

        #region winform
        public static void AutoSetFormIcon(Form form)
        {
#if DEBUG
            form.Icon = Properties.Resources.icon_light;
#else
            form.Icon = Properties.Resources.icon_dark;
#endif
        }

        public static System.Drawing.Icon GetAppIcon()
        {
#if DEBUG
            return Properties.Resources.icon_light;
#else
            return Properties.Resources.icon_dark;
#endif
        }
        #endregion
    }
}
