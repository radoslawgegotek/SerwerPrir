using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Serwer.Services
{
    public class TaskQueue
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<Task> _tasks;

        public TaskQueue(int concurrencyLevel)
        {
            _semaphore = new SemaphoreSlim(concurrencyLevel);
            _tasks = new ConcurrentBag<Task>();
        }

        public async Task AddTask(Func<Task> taskGenerator)
        {
            Console.WriteLine("Adding task to queue.");
            await _semaphore.WaitAsync();
            Console.WriteLine("Semaphore acquired.");

            var task = Task.Run(async () =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"Running task on thread {threadId}");

                try
                {
                    await taskGenerator();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in task: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                    Console.WriteLine("Semaphore released.");
                }
            });

            _tasks.Add(task);
            Console.WriteLine("Task added to the task list.");
        }

        public async Task WhenAllTasksComplete()
        {
            Console.WriteLine("Waiting for all tasks to complete.");

            var tasksArray = _tasks.ToArray();
            if (tasksArray.Length == 0)
            {
                Console.WriteLine("No tasks to wait for.");
                return;
            }

            try
            {
                await Task.WhenAll(tasksArray);
                Console.WriteLine("All tasks in TaskQueue completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in TaskQueue: {ex.Message}");
            }
        }
    }
}
