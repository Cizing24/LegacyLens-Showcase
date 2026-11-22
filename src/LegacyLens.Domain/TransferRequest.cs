using System;

namespace LegacyLens.Domain
{
    public sealed class TransferRequest
    {
        public Guid RequestId { get; set; }
        public int CaseId { get; set; }
        public int ToAssignee { get; set; }
        public string Reason { get; set; }
        public byte[] Version { get; set; }
    }
}
