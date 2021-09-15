using System;
using System.Threading.Tasks;

namespace ConsoleHost
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"id:{Id},name:{Name}";
        }
    }

    public class DurableFunctionAttribute : Attribute
    {
    }

    public enum BugLevel
    {
        Low,
        Middle,
        High,
    }


    public enum BugStatus
    {
        Requested,
        Accepted,
        Rejected
    }

    public class BugInfo
    {
        public string Title { get; set; }
        public BugLevel Level { get; set; }
    }

    public class AcceptInfo
    {
        public string Comment { get; set; }
    }

    public class RejectInfo
    {
        public string Reason { get; set; }
    }

    public abstract class CommonProcess
    {
        public bool Completed { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateAt { get; set; }
    }

    public abstract class Bug : CommonProcess
    {
        public string Title { get; set; }
        public BugLevel Level { get; set; }
        public BugStatus Status { get; set; }

        [DurableFunction]
        public async Task Run()
        {
            var bugInfo = await GetBugInfo();
            CreateAt = DateTime.Now;
            CreateBy = GetCurrentUser();
            Completed = false;
            Title = bugInfo.Title;
            Level = bugInfo.Level;
            Status = BugStatus.Requested;
            await Task.WhenAny(Accept(), Reject());
            Completed = true;
        }

        private async Task Accept()
        {
            var acceptInfo = await GetAcceptInfo();
            Status = BugStatus.Accepted;
        }

        private async Task Reject()
        {
            var rejectInfo = await GetRejectInfo();
            Status = BugStatus.Accepted;
        }

        protected abstract Task<BugInfo> GetBugInfo();
        protected abstract Task<AcceptInfo> GetAcceptInfo();
        protected abstract Task<RejectInfo> GetRejectInfo();

        protected abstract string GetCurrentUser();
    }
}