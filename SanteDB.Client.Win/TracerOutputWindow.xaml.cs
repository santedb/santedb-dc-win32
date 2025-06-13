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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SanteDB.Client.Win
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TracerOutputWindow : Window
    {
        public TracerOutputWindow()
        {
            this.InitializeComponent();

            ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            _AutoScroll = true;
        }

        private Paragraph _CurrentPara;

        private bool _AutoScroll;
        private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            //if (e.IsIntermediate)
            //{
            //    return;
            //}

            //if (ScrollViewer.VerticalOffset == ScrollViewer.ScrollableHeight)
            //{
            //    _AutoScroll = true;
            //}
            //else
            //{
            //    _AutoScroll = false;
            //}

            //if (_AutoScroll)
            //{
            //    ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
            //}
        }

        public void AppendText(string text)
        {


            if (DispatcherQueue.HasThreadAccess)
            {
                AppendTextInternal(text);
                if (_AutoScroll)
                {
                    ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
                }
            }
            else
            {
                string txt = text;
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppendTextInternal(txt);
                    if (_AutoScroll)
                    {
                        ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
                    }
                });
            }

        }

        private void AppendTextInternal(string text)
        {
            if (null == _CurrentPara)
            {
                _CurrentPara = new Paragraph();
                TextBlock.Blocks.Add(_CurrentPara);
            }

            var run = new Run();
            run.Text = text;

            _CurrentPara.Inlines.Add(run);
            _CurrentPara.Inlines.Add(new LineBreak());
            
        }
    }
}
