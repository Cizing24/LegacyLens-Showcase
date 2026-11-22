using System;
using LegacyLens.Domain;

namespace LegacyLens.Application
{
    public static class TransferRules
    {
        public static void Validate(TransferRequest request, Actor actor, CaseRecord current)
        {
            if (actor == null || !actor.CanManage) throw new UnauthorizedAccessException("案件轉派需要主管權限。");
            if (request == null || request.RequestId == Guid.Empty || request.CaseId < 1 || request.ToAssignee < 1)
                throw new ArgumentException("轉派要求無效，請重新載入案件。");
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5 || request.Reason.Trim().Length > 500)
                throw new ArgumentException("請填寫 5～500 字的轉派原因。");
            if (request.Version == null || request.Version.Length != 8) throw new ArgumentException("缺少編輯版本，請重新載入。");
            if (current == null || current.Id != request.CaseId) throw new ArgumentException("找不到案件。");
            if (current.Status == "Closed") throw new ArgumentException("已結案案件不能轉派，請先由主管重新開啟。");
            if (current.AssignedTo == request.ToAssignee) throw new ArgumentException("新承辦人須與目前承辦人不同。");
        }
    }
}
