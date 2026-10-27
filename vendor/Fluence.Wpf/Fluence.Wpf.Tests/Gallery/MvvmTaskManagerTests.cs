/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fluence.Wpf.Demo.Mvvm.ViewModels;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery
{
    public sealed class MvvmTaskManagerTests
    {
        [Fact]
        public void AddCommand_BlankInputDisabled_ValidInputTrimmedAndReset()
        {
            MainViewModel vm = CreateEmptyViewModel();
            Assert.False(vm.AddCommand.CanExecute(parameter: null));
            vm.NewTaskText = "   ";
            Assert.False(vm.AddCommand.CanExecute(parameter: null));

            vm.NewTaskText = "  Review task  ";
            Assert.True(vm.AddCommand.CanExecute(parameter: null));
            vm.AddCommand.Execute(parameter: null);

            Assert.Equal("Review task", Assert.Single(vm.DisplayedTasks).Title);
            Assert.Equal(string.Empty, vm.NewTaskText);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{0} of {1} completed", 0, 1), vm.StatusText);
            Assert.Equal(0d, vm.ProgressValue);
            Assert.False(vm.AddCommand.CanExecute(parameter: null));
        }

        [Fact]
        public void FiltersAndCompletion_NotifyAfterDisplayedTasksRebuild()
        {
            MainViewModel vm = CreateEmptyViewModel();
            vm.NewTaskText = "First";
            vm.AddCommand.Execute(parameter: null);
            vm.NewTaskText = "Second";
            vm.AddCommand.Execute(parameter: null);
            TaskItemViewModel first = vm.DisplayedTasks[0];
            TaskItemViewModel second = vm.DisplayedTasks[1];
            first.IsCompleted = true;
            Assert.Equal(2, vm.DisplayedTasks.Count);
            vm.ActiveFilter = FilterMode.Completed;
            Assert.Same(first, Assert.Single(vm.DisplayedTasks));
            vm.ActiveFilter = FilterMode.Pending;
            Assert.Same(second, Assert.Single(vm.DisplayedTasks));

            List<string> notifications = [];
            vm.PropertyChanged += (_, args) =>
            {
                string? name = args.PropertyName;
                if (name is not null
                    && (string.Equals(name, nameof(MainViewModel.StatusText), StringComparison.Ordinal)
                        || string.Equals(name, nameof(MainViewModel.ProgressValue), StringComparison.Ordinal)))
                {
                    Assert.Empty(vm.DisplayedTasks);
                    Assert.Equal(100d, vm.ProgressValue);
                    notifications.Add(name);
                }
            };
            second.IsCompleted = true;

            Assert.Contains(nameof(MainViewModel.StatusText), notifications, StringComparer.Ordinal);
            Assert.Contains(nameof(MainViewModel.ProgressValue), notifications, StringComparer.Ordinal);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{0} of {1} completed", 2, 2), vm.StatusText);
        }

        [Fact]
        public void DeleteAndClearCompleted_DetachRemovedItems()
        {
            MainViewModel vm = CreateEmptyViewModel();
            foreach (string title in new[] { "Deleted", "Cleared", "Surviving" })
            {
                vm.NewTaskText = title;
                vm.AddCommand.Execute(parameter: null);
            }
            TaskItemViewModel deleted = vm.DisplayedTasks[0];
            TaskItemViewModel cleared = vm.DisplayedTasks[1];
            TaskItemViewModel survivor = vm.DisplayedTasks[2];
            vm.DeleteCommand.Execute(deleted);
            cleared.IsCompleted = true;
            vm.ClearCompletedCommand.Execute(parameter: null);
            int notifications = 0;
            vm.PropertyChanged += (_, _) => notifications++;

            deleted.IsCompleted = true;
            cleared.IsCompleted = false;

            Assert.Equal(0, notifications);
            Assert.Same(survivor, Assert.Single(vm.DisplayedTasks));
            vm.DeleteCommand.Execute(survivor);
            Assert.Equal("No tasks", vm.StatusText);
            Assert.Equal(0d, vm.ProgressValue);
        }

        private static MainViewModel CreateEmptyViewModel()
        {
            MainViewModel vm = new();
            foreach (TaskItemViewModel item in vm.DisplayedTasks.ToArray())
            {
                vm.DeleteCommand.Execute(item);
            }
            return vm;
        }
    }
}
#endif
