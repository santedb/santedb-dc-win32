﻿/*
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
 * Date: 2023-7-6
 */
using SharpCompress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Win.ViewModels
{
    internal static class Extensions
    {
        public static void AddTask(this MainWindowViewModel viewModel, string? identifier, string? text, float? progress)
        {
            if (null == viewModel)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            viewModel.BackgroundTasks.Add(new BackgroundTaskStatus
            {
                Dismissed = false,
                Progress = progress ?? -1,
                TaskIdentifier = identifier,
                Text = text,
            });
        }

        public static void RemoveTask(this MainWindowViewModel viewModel, string identifier)
        {
            if (null == viewModel)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            var tasks = viewModel.BackgroundTasks.Where(t => t.TaskIdentifier == identifier).Select((t, i) => i).ToArray();

            foreach (var taskid in tasks)
                viewModel.BackgroundTasks.RemoveAt(taskid);
        }
    }
}
