﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using WslManager.Extensions;
using WslManager.ViewModels;

namespace WslManager.Screens.MainForm
{
    // Backup Worker
    partial class MainForm
    {
        private BackgroundWorker backupWorker;

        partial void InitializeBackupWorker(IContainer components)
        {
            backupWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };
            components.Add(backupWorker);

            backupWorker.DoWork += BackupWorker_DoWork;
            backupWorker.ProgressChanged += BackupWorker_ProgressChanged;
            backupWorker.RunWorkerCompleted += BackupWorker_RunWorkerCompleted;
        }

        private void BackupWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var request = (DistroBackupRequest)e.Argument;
            var process = request.CreateExportDistroProcess(request.SaveFilePath);
            process.Start();

            var list = WslExtensions.GetDistroList();
            var convertingItem = list.Where(x => string.Equals(x.DistroName, request.DistroName, StringComparison.Ordinal)).FirstOrDefault();
            backupWorker.ReportProgress(0, convertingItem);

            while (!process.HasExited && !backupWorker.CancellationPending)
            {
                list = WslExtensions.GetDistroList();
                convertingItem = list.Where(x => string.Equals(x.DistroName, request.DistroName, StringComparison.Ordinal)).FirstOrDefault();
                backupWorker.ReportProgress(50, convertingItem);
                Thread.Sleep(TimeSpan.FromSeconds(1d));
            }

            list = WslExtensions.GetDistroList();
            convertingItem = list.Where(x => string.Equals(x.DistroName, request.DistroName, StringComparison.Ordinal)).FirstOrDefault();
            backupWorker.ReportProgress(100, convertingItem);
            request.Succeed = true;
            e.Result = request;
        }

        private void BackupWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (IsDisposed)
                return;

            var targetItem = (DistroInfo)e.UserState;

            foreach (ListViewItem eachItem in listView.Items)
            {
                var boundItem = (DistroInfo)eachItem.Tag;

                if (!string.Equals(boundItem.DistroName, targetItem.DistroName))
                    continue;

                eachItem.SubItems["status"].Text = targetItem.DistroStatus;
                break;
            }
        }

        private void BackupWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsDisposed)
                return;

            if (e.Error != null)
            {
                MessageBox.Show(this,
                    "Unexpected error occurred. " + e.Error.Message,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "User cancelled the task",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = e.Result as DistroBackupRequest;

            if (result == null)
            {
                MessageBox.Show(this,
                    "Cannot obtain task result. It seems like a bug.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (result.Succeed)
            {
                //RefreshListView(listView, statusItem, WslExtensions.GetDistroList());
                var itemPath = result.SaveFilePath.Replace(@"/", @"\");
                Process.Start("explorer.exe", "/select," + itemPath);
            }
            else
            {
                MessageBox.Show(this,
                    "Task does not succeed.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
