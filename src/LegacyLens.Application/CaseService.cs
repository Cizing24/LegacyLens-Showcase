using System;
using LegacyLens.Domain;

namespace LegacyLens.Application
{
    public interface ICaseStore
    {
        CaseRecord Get(int id);
        int Create(CaseRecord item, Actor actor, string correlationId);
        void Update(CaseRecord item, Actor actor, string correlationId);
    }

    public static class CaseRules
    {
        public static void Validate(CaseRecord item, Actor actor, CaseRecord existing)
        {
            if (actor == null || !actor.CanEdit) throw new UnauthorizedAccessException();
            if (item == null) throw new ArgumentException("請填寫案件資料。");
            if (string.IsNullOrWhiteSpace(item.Title) || item.Title.Trim().Length > 160)
                throw new ArgumentException("案件主旨必須為 1～160 個字。");
            if ((item.Description ?? "").Length > 2000) throw new ArgumentException("說明最多 2,000 個字。");
            if (item.CustomerId < 1 || item.AssignedTo < 1) throw new ArgumentException("請選擇客戶與承辦人。");
            if (item.Status != "Open" && item.Status != "InProgress" && item.Status != "Closed")
                throw new ArgumentException("案件狀態無效。");
            if (existing != null && !actor.CanManage && existing.AssignedTo != actor.Id)
                throw new UnauthorizedAccessException("僅能編輯自己負責的案件。");
            if (!actor.CanManage && item.AssignedTo != actor.Id)
                throw new UnauthorizedAccessException("案件指派需要主管權限。");
            if (existing == null && item.Status != "Open") throw new ArgumentException("新案件必須為待處理。");
            if (existing != null && existing.Status == "Closed" && item.Status != "Closed" && !actor.CanManage)
                throw new UnauthorizedAccessException("重新開啟案件需要主管權限。");
            if (existing != null && item.AssignedTo != existing.AssignedTo)
                throw new ArgumentException("請使用案件轉派，並填寫轉派原因。");
        }
    }

    public sealed class CaseService
    {
        private readonly ICaseStore store;
        public CaseService(ICaseStore store) { this.store = store; }
        public int Save(CaseRecord item, Actor actor, string correlationId)
        {
            if (item == null) throw new ArgumentException("請填寫案件資料。");
            CaseRules.Validate(item, actor, item.Id == 0 ? null : store.Get(item.Id));
            item.Title = item.Title.Trim();
            if (item.Id == 0) return store.Create(item, actor, correlationId);
            store.Update(item, actor, correlationId);
            return item.Id;
        }
    }
}
