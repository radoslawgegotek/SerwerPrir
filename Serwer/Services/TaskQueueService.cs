//namespace Serwer.Services
//{
//    public class TaskQueueService : BackgroundService
//    {
//        private readonly TaskQueue _taskQueue;

//        public TaskQueueService(TaskQueue taskQueue)
//        {
//            _taskQueue = taskQueue;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

//                try
//                {
//                    await workItem(stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    // Log the exception
//                    Console.WriteLine($"Error occurred: {ex.Message}");
//                }
//            }
//        }

//    }
//}
