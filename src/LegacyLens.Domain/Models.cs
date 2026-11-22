using System;

namespace LegacyLens.Domain
{
    public sealed class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool CanManage { get { return Role == "Manager"; } }
        public bool CanEdit { get { return Role == "Agent" || CanManage; } }
    }

    public sealed class CaseRecord
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] Version { get; set; }
    }

    public sealed class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Industry { get; set; }
        public byte[] Version { get; set; }
    }

    public sealed class ConflictException : Exception
    {
        public ConflictException() : base("資料已被其他人更新，請重新載入後再試。") { }
    }
}
