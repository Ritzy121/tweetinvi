﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi.Core.ExecutionContext;
using Tweetinvi.Core.Helpers;

namespace Tweetinvi
{
    /// <summary>
    /// Async made easy.
    /// </summary>
    public static class Sync
    {
        private static ICrossExecutionContextPreparer _crossExecutionContextPreparer;
        private static ITaskFactory _taskFactory;

        private static void init()
        {
            // No need for locking, they're singletons anyway so worst case scenario the same object
            //  gets resolved multiple times
            if (_crossExecutionContextPreparer == null || _taskFactory == null)
            {
                _crossExecutionContextPreparer = TweetinviContainer.Resolve<ICrossExecutionContextPreparer>();
                _taskFactory = TweetinviContainer.Resolve<ITaskFactory>();
            }
        }

        /// <summary>
        /// Execute a task asynchronously with Tweetinvi
        /// </summary>
        public static Task ExecuteTaskAsync(Action action)
        {
            init();
            return _taskFactory.ExecuteTaskAsync(action);
        }

        /// <summary>
        /// Execute a task asynchronously with Tweetinvi
        /// </summary>
        public static Task<T> ExecuteTaskAsync<T>(Func<T> func)
        {
            init();
            return _taskFactory.ExecuteTaskAsync(func);
        }

        /// <summary>
        /// Prepare the current Task for Tweetinvi to be used asynchronously within it
        /// </summary>
        public static void PrepareForAsync()
        {
            init();
            _crossExecutionContextPreparer.Prepare();
        }

        /// <summary>
        /// Execute a task asynchronously with Tweetinvi independently of the calling context
        /// </summary>
        public static Task ExecuteIsolatedTaskAsync(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // Prevent execution context from being passed over to the new thread
            Task t;
            using (ExecutionContext.SuppressFlow())
            {
                t = Task.Run(action);
            }
            return t;
        }

        /// <summary>
        /// Execute a task asynchronously with Tweetinvi independently of the calling context
        /// </summary>
        public static Task<T> ExecuteIsolatedTaskAsync<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            // Prevent execution context from being passed over to the new thread
            Task<T> t;
            using (ExecutionContext.SuppressFlow())
            {
                t = Task.Run(func);
            }
            return t;
        }
    }
}