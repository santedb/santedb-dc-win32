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
