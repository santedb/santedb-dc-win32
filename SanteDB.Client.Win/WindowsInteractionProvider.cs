/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: trevor
 * Date: 2023-4-19
 */
using SanteDB.Client.Upstream.Management;
using SanteDB.Client.UserInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.WinUI
{
    public class WindowsInteractionProvider : IUserInterfaceInteractionProvider
    {
        private static readonly string s_DefaultTaskIdentifier = string.Empty;

        public string ServiceName => "Windows Interaction Provider";

        readonly MainWindow m_MainWindow;

        public WindowsInteractionProvider(MainWindow mainWindow)
        {
            m_MainWindow = mainWindow;
        }

        public void Alert(string message)
        {
            Nito.AsyncEx.AsyncContext.Run(async () => await m_MainWindow.ShowAlertAsync(message));
        }

        public bool Confirm(string message)
        {
            return Nito.AsyncEx.AsyncContext.Run(() => m_MainWindow.ShowConfirmAsync(message));
        }

        public string? Prompt(string message, bool maskEntry = false)
        {
            return Nito.AsyncEx.AsyncContext.Run(() => m_MainWindow.ShowPromptAsync(message, maskInput: maskEntry));
        }

        public void SetStatus(string statusText, float progressIndicator)
            => SetStatus(s_DefaultTaskIdentifier, statusText, progressIndicator);

        public void SetStatus(string taskIdentifier, string statusText, float progressIndicator)
        {
            //m_MainWindow.ShowSplashStatusText($"Starting SanteDB - {Math.Round(progressIndicator, 2)} :: {taskIdentifier}");
            m_MainWindow.SetStatus(taskIdentifier, statusText, progressIndicator);
        }



        public Stream SelectFile(string title, string pattern, string path)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            if (!string.IsNullOrEmpty(pattern))
            {
                var patterns = pattern.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var ptrn in patterns)
                {
                    if (ptrn.IndexOf('/') == -1)
                        picker.FileTypeFilter.Add(ptrn);
                }
            }

            return Nito.AsyncEx.AsyncContext.Run(async () => {
                var pickerResult = await picker.PickSingleFileAsync();
                if (null != pickerResult?.Path)
                {
                    return (await pickerResult.OpenReadAsync())?.AsStreamForRead();
                }
                else
                {
                    return null;
                }
            });

        }

        /// <summary>
        /// TODO: Clean this up and test
        /// </summary>
        public string SaveFile(string defaultPath, string defaultName, Stream fileContents)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedFileName = defaultName;
            picker.DefaultFileExtension = Path.GetExtension(defaultName);

            var result = Nito.AsyncEx.AsyncContext.Run(async () => await picker.PickSaveFileAsync());
            if(!String.IsNullOrEmpty(result.Path))
            {
                using(var fs = new FileStream(result.Path, FileMode.Create))
                {
                    fileContents.CopyTo(fs);
                }
            }
            return result.Path;
        }
    }
}
